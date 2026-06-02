using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Places;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using FluentValidation;

namespace Adondeamos.Application.Services;

/// <summary>Lugares: búsqueda (Autocomplete), resolución de lugares de Google y alta de lugares propios.</summary>
public sealed class PlaceService
{
    private readonly IPlaceRepository _places;
    private readonly IGooglePlacesClient _google;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ResolvePlaceRequest> _resolveValidator;
    private readonly IValidator<CreateOwnPlaceRequest> _createOwnValidator;

    public PlaceService(
        IPlaceRepository places,
        IGooglePlacesClient google,
        IUnitOfWork unitOfWork,
        IValidator<ResolvePlaceRequest> resolveValidator,
        IValidator<CreateOwnPlaceRequest> createOwnValidator)
    {
        _places = places;
        _google = google;
        _unitOfWork = unitOfWork;
        _resolveValidator = resolveValidator;
        _createOwnValidator = createOwnValidator;
    }

    public async Task<IReadOnlyList<PlacePrediction>> SearchAsync(string? query, string? sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ValidationException("Debes indicar el término de búsqueda 'q'.");
        }

        return await _google.AutocompleteAsync(query.Trim(), sessionToken, cancellationToken);
    }

    public async Task<ResolvePlaceResponse> ResolveAsync(Guid userId, ResolvePlaceRequest request, CancellationToken cancellationToken = default)
    {
        await _resolveValidator.ValidateAndThrowAsync(request, cancellationToken);

        var googlePlaceId = request.GooglePlaceId.Trim();

        // Detalles bajo demanda: además de mostrarse, validan que el place_id exista en Google.
        var details = await _google.GetDetailsAsync(googlePlaceId, request.SessionToken, cancellationToken)
            ?? throw new NotFoundException("No se encontró el lugar en Google.");

        // Canoniza por place_id: si ya existe, se devuelve el mismo registro (no se duplica).
        var existing = await _places.GetByGooglePlaceIdAsync(googlePlaceId, cancellationToken);

        Place place;
        if (existing is not null)
        {
            place = existing;
        }
        else
        {
            var created = new Place
            {
                Origin = PlaceOrigin.Google,
                GooglePlaceId = googlePlaceId,
                CreatedBy = userId
            };
            _places.Add(created);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                place = created;
            }
            catch (ConflictException)
            {
                // Otra petición lo insertó al mismo tiempo: recupera el existente y sigue.
                place = await _places.GetByGooglePlaceIdAsync(googlePlaceId, cancellationToken)
                    ?? throw new ConflictException("No se pudo resolver el lugar de Google.");
            }
        }

        return new ResolvePlaceResponse(PlaceResponse.FromEntity(place), details);
    }

    public async Task<PlaceResponse> CreateOwnAsync(Guid userId, CreateOwnPlaceRequest request, CancellationToken cancellationToken = default)
    {
        await _createOwnValidator.ValidateAndThrowAsync(request, cancellationToken);

        var place = new Place
        {
            Origin = PlaceOrigin.Own,
            Name = request.Name.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            CreatedBy = userId
        };

        _places.Add(place);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PlaceResponse.FromEntity(place);
    }
}
