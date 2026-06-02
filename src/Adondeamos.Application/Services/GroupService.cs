using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Groups;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using FluentValidation;

namespace Adondeamos.Application.Services;

/// <summary>Grupos opcionales (pareja, familia, amigos) para compartir.</summary>
public sealed class GroupService
{
    private readonly IGroupRepository _groups;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateGroupRequest> _createValidator;
    private readonly IValidator<AddMemberRequest> _addMemberValidator;

    public GroupService(
        IGroupRepository groups,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IValidator<CreateGroupRequest> createValidator,
        IValidator<AddMemberRequest> addMemberValidator)
    {
        _groups = groups;
        _users = users;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _addMemberValidator = addMemberValidator;
    }

    public async Task<GroupDetailResponse> CreateGroupAsync(Guid userId, CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var group = new Group
        {
            Name = request.Name.Trim(),
            CreatedBy = userId
        };
        // El creador entra como owner. EF asigna el group_id al miembro tras generar el id del grupo.
        group.Members.Add(new GroupMember { UserId = userId, Role = GroupRole.Owner });

        _groups.Add(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetGroupAsync(userId, group.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupResponse>> GetGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await _groups.GetMembershipsForUserAsync(userId, cancellationToken);

        return memberships
            .OrderByDescending(m => m.Group.CreatedAt)
            .Select(GroupResponse.FromMembership)
            .ToList();
    }

    public async Task<GroupDetailResponse> GetGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetDetailAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Grupo no encontrado.");

        EnsureMember(group, userId);

        return GroupDetailResponse.FromEntity(group);
    }

    public async Task<GroupMemberResponse> AddMemberAsync(Guid requesterUserId, Guid groupId, AddMemberRequest request, CancellationToken cancellationToken = default)
    {
        await _addMemberValidator.ValidateAndThrowAsync(request, cancellationToken);

        var group = await _groups.GetDetailAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Grupo no encontrado.");

        // Solo un miembro del grupo puede agregar a otros.
        EnsureMember(group, requesterUserId);

        var target = request.UserId.HasValue
            ? await _users.GetByIdAsync(request.UserId.Value, cancellationToken)
            : await _users.GetByEmailAsync(request.Email!.Trim(), cancellationToken);

        if (target is null)
        {
            throw new NotFoundException("No se encontró el usuario a agregar.");
        }

        if (group.Members.Any(m => m.UserId == target.Id))
        {
            throw new ConflictException("Ese usuario ya es miembro del grupo.");
        }

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = target.Id,
            Role = GroupRole.Member
        };

        _groups.AddMember(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // member.JoinedAt lo generó la base; el usuario lo adjuntamos para armar la respuesta.
        member.User = target;
        return GroupMemberResponse.FromEntity(member);
    }

    private static void EnsureMember(Group group, Guid userId)
    {
        if (!group.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenException("No perteneces a este grupo.");
        }
    }
}
