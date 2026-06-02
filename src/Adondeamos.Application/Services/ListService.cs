using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Lists;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using FluentValidation;

namespace Adondeamos.Application.Services;

/// <summary>Listas personales o de grupo, y sus elementos (guardados).</summary>
public sealed class ListService
{
    private readonly IListRepository _lists;
    private readonly ISaveRepository _saves;
    private readonly IGroupRepository _groups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateListRequest> _createValidator;
    private readonly IValidator<AddListItemRequest> _addItemValidator;

    public ListService(
        IListRepository lists,
        ISaveRepository saves,
        IGroupRepository groups,
        IUnitOfWork unitOfWork,
        IValidator<CreateListRequest> createValidator,
        IValidator<AddListItemRequest> addItemValidator)
    {
        _lists = lists;
        _saves = saves;
        _groups = groups;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _addItemValidator = addItemValidator;
    }

    public async Task<ListResponse> CreateListAsync(Guid userId, CreateListRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (request.GroupId is Guid groupId && !await _groups.IsMemberAsync(groupId, userId, cancellationToken))
        {
            throw new ForbiddenException("No perteneces al grupo indicado.");
        }

        var list = new List
        {
            OwnerId = userId,
            GroupId = request.GroupId,
            Name = request.Name.Trim(),
            Visibility = request.Visibility ?? ContentVisibility.Private
        };

        _lists.Add(list);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ListResponse.FromEntity(list);
    }

    public async Task<IReadOnlyList<ListResponse>> GetListsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var lists = await _lists.GetForUserAsync(userId, cancellationToken);
        return lists.Select(ListResponse.FromEntity).ToList();
    }

    public async Task<ListDetailResponse> GetListAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        var list = await _lists.GetDetailAsync(listId, cancellationToken)
            ?? throw new NotFoundException("Lista no encontrada.");

        await EnsureCanAccessAsync(list, userId, cancellationToken);

        return ListDetailResponse.FromEntity(list);
    }

    public async Task<ListItemResponse> AddItemAsync(Guid userId, Guid listId, AddListItemRequest request, CancellationToken cancellationToken = default)
    {
        await _addItemValidator.ValidateAndThrowAsync(request, cancellationToken);

        var list = await _lists.GetByIdAsync(listId, cancellationToken)
            ?? throw new NotFoundException("Lista no encontrada.");

        await EnsureCanAccessAsync(list, userId, cancellationToken);

        var save = await _saves.GetByIdWithPlaceAsync(request.SaveId, cancellationToken)
            ?? throw new NotFoundException("Guardado no encontrado.");

        // Cada quien agrega sus propios guardados (en listas de grupo se juntan los de varios dueños).
        if (save.UserId != userId)
        {
            throw new ForbiddenException("Solo puedes agregar tus propios guardados a una lista.");
        }

        if (await _lists.ItemExistsAsync(listId, save.Id, cancellationToken))
        {
            throw new ConflictException("Ese guardado ya está en la lista.");
        }

        var item = new ListItem
        {
            ListId = listId,
            SaveId = save.Id,
            AddedBy = userId,
            Position = await _lists.GetNextPositionAsync(listId, cancellationToken)
        };

        _lists.AddItem(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        item.Save = save;
        return ListItemResponse.FromEntity(item);
    }

    public async Task RemoveItemAsync(Guid userId, Guid listId, Guid saveId, CancellationToken cancellationToken = default)
    {
        var item = await _lists.GetItemAsync(listId, saveId, cancellationToken)
            ?? throw new NotFoundException("Ese guardado no está en la lista.");

        var list = await _lists.GetByIdAsync(listId, cancellationToken)
            ?? throw new NotFoundException("Lista no encontrada.");

        // Puede quitarlo el dueño de la lista o quien agregó el elemento.
        if (list.OwnerId != userId && item.AddedBy != userId)
        {
            throw new ForbiddenException("No puedes quitar este elemento de la lista.");
        }

        _lists.RemoveItem(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Acceso a una lista: su dueño o un miembro del grupo (si es lista de grupo).</summary>
    private async Task EnsureCanAccessAsync(List list, Guid userId, CancellationToken cancellationToken)
    {
        if (list.OwnerId == userId)
        {
            return;
        }

        if (list.GroupId is Guid groupId && await _groups.IsMemberAsync(groupId, userId, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("No tienes acceso a esta lista.");
    }
}
