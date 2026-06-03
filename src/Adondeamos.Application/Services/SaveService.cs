using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Saves;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using FluentValidation;

namespace Adondeamos.Application.Services;

/// <summary>Guardados, que pertenecen siempre a un usuario.</summary>
public sealed class SaveService
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"];
    private const long MaxPhotoBytes = 10 * 1024 * 1024; // 10 MB

    private readonly ISaveRepository _saves;
    private readonly IPlaceRepository _places;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSaveRequest> _createValidator;
    private readonly IValidator<UpdateSaveRequest> _updateValidator;
    private readonly IPhotoStorage _photoStorage;

    public SaveService(
        ISaveRepository saves,
        IPlaceRepository places,
        IUnitOfWork unitOfWork,
        IValidator<CreateSaveRequest> createValidator,
        IValidator<UpdateSaveRequest> updateValidator,
        IPhotoStorage photoStorage)
    {
        _saves = saves;
        _places = places;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _photoStorage = photoStorage;
    }

    public async Task<SaveResponse> CreateSaveAsync(Guid userId, CreateSaveRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var place = await _places.GetByIdAsync(request.PlaceId, cancellationToken)
            ?? throw new NotFoundException("El lugar no existe.");

        if (await _saves.ExistsForUserAndPlaceAsync(userId, place.Id, cancellationToken))
        {
            throw new ConflictException("Ya guardaste este lugar.");
        }

        var save = new Save
        {
            UserId = userId,
            PlaceId = place.Id,
            SourceNetwork = request.SourceNetwork ?? SocialNetwork.Manual,
            SourceUrl = Clean(request.SourceUrl),
            ThumbnailUrl = Clean(request.ThumbnailUrl),
            Note = Clean(request.Note),
            Visibility = request.Visibility ?? ContentVisibility.Private,
            Status = SaveStatus.Pending
        };

        _saves.Add(save);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        save.Place = place;
        return SaveResponse.FromEntity(save);
    }

    public async Task<IReadOnlyList<SaveResponse>> GetSavesAsync(Guid userId, SaveStatus? status, Guid? listId, CancellationToken cancellationToken = default)
    {
        var saves = await _saves.GetForUserAsync(userId, status, listId, cancellationToken);
        return saves.Select(SaveResponse.FromEntity).ToList();
    }

    public async Task<SaveResponse> UpdateSaveAsync(Guid userId, Guid saveId, UpdateSaveRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var save = await _saves.GetByIdWithPlaceAsync(saveId, cancellationToken)
            ?? throw new NotFoundException("Guardado no encontrado.");

        if (save.UserId != userId)
        {
            throw new ForbiddenException("Este guardado no es tuyo.");
        }

        if (request.Note is not null)
        {
            save.Note = Clean(request.Note);
        }

        if (request.Visibility is not null)
        {
            save.Visibility = request.Visibility.Value;
        }

        if (request.Visited is not null)
        {
            if (request.Visited.Value)
            {
                save.Status = SaveStatus.Visited;
                save.VisitedAt ??= DateTime.UtcNow;
            }
            else
            {
                save.Status = SaveStatus.Pending;
                save.VisitedAt = null;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SaveResponse.FromEntity(save);
    }

    public async Task DeleteSaveAsync(Guid userId, Guid saveId, CancellationToken cancellationToken = default)
    {
        var save = await _saves.GetByIdAsync(saveId, cancellationToken)
            ?? throw new NotFoundException("Guardado no encontrado.");

        if (save.UserId != userId)
        {
            throw new ForbiddenException("Este guardado no es tuyo.");
        }

        _saves.Remove(save);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sube o reemplaza la foto de portada del guardado. El stream no se cierra aquí.
    /// </summary>
    public async Task<PhotoUploadResponse> UploadPhotoAsync(
        Guid userId, Guid saveId, Stream photoStream, string contentType, long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new ValidationException($"Tipo de archivo no permitido: {contentType}. Solo se aceptan imágenes (JPEG, PNG, WebP, GIF).");
        }

        if (fileSize > MaxPhotoBytes)
        {
            throw new ValidationException($"La imagen no puede superar los 10 MB.");
        }

        var save = await _saves.GetByIdWithPlaceAsync(saveId, cancellationToken)
            ?? throw new NotFoundException("Guardado no encontrado.");

        if (save.UserId != userId)
        {
            throw new ForbiddenException("Este guardado no es tuyo.");
        }

        // Elimina la foto anterior si era nuestra (evita huérfanos en el almacenamiento).
        if (save.ThumbnailUrl is not null)
        {
            var oldKey = _photoStorage.TryGetKeyFromUrl(save.ThumbnailUrl);
            if (oldKey is not null)
            {
                await _photoStorage.DeleteAsync(oldKey, cancellationToken);
            }
        }

        var ext = ExtensionFromContentType(contentType);
        var key = $"saves/{saveId}/cover.{ext}";
        var newUrl = await _photoStorage.UploadAsync(key, photoStream, contentType, cancellationToken);

        save.ThumbnailUrl = newUrl;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PhotoUploadResponse(saveId, newUrl);
    }

    /// <summary>Elimina la foto de portada del guardado y limpia el campo.</summary>
    public async Task DeletePhotoAsync(Guid userId, Guid saveId, CancellationToken cancellationToken = default)
    {
        var save = await _saves.GetByIdAsync(saveId, cancellationToken)
            ?? throw new NotFoundException("Guardado no encontrado.");

        if (save.UserId != userId)
        {
            throw new ForbiddenException("Este guardado no es tuyo.");
        }

        if (save.ThumbnailUrl is not null)
        {
            var key = _photoStorage.TryGetKeyFromUrl(save.ThumbnailUrl);
            if (key is not null)
            {
                await _photoStorage.DeleteAsync(key, cancellationToken);
            }

            save.ThumbnailUrl = null;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string ExtensionFromContentType(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "jpg"
        };
}
