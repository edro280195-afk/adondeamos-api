using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Invitations;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using FluentValidation;

namespace Adondeamos.Application.Services;

/// <summary>
/// Invitaciones a grupos. Agregar a alguien no es directo: se le invita y debe aceptar
/// para entrar a group_members.
/// </summary>
public sealed class InvitationService
{
    private readonly IInvitationRepository _invitations;
    private readonly IGroupRepository _groups;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<InviteMemberRequest> _inviteValidator;

    public InvitationService(
        IInvitationRepository invitations,
        IGroupRepository groups,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IValidator<InviteMemberRequest> inviteValidator)
    {
        _invitations = invitations;
        _groups = groups;
        _users = users;
        _unitOfWork = unitOfWork;
        _inviteValidator = inviteValidator;
    }

    public async Task<InvitationResponse> InviteAsync(Guid requesterUserId, Guid groupId, InviteMemberRequest request, CancellationToken cancellationToken = default)
    {
        await _inviteValidator.ValidateAndThrowAsync(request, cancellationToken);

        var group = await _groups.GetDetailAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Grupo no encontrado.");

        // Solo un miembro del grupo puede invitar.
        if (!group.Members.Any(m => m.UserId == requesterUserId))
        {
            throw new ForbiddenException("No perteneces a este grupo.");
        }

        var target = request.UserId.HasValue
            ? await _users.GetByIdAsync(request.UserId.Value, cancellationToken)
            : await _users.GetByEmailAsync(request.Email!.Trim(), cancellationToken);

        if (target is null)
        {
            throw new NotFoundException("No se encontró el usuario a invitar.");
        }

        if (group.Members.Any(m => m.UserId == target.Id))
        {
            throw new ConflictException("Ese usuario ya es miembro del grupo.");
        }

        if (await _invitations.HasPendingAsync(groupId, target.Id, cancellationToken))
        {
            throw new ConflictException("Ya hay una invitación pendiente para ese usuario.");
        }

        var invitation = new GroupInvitation
        {
            GroupId = groupId,
            InvitedUser = target.Id,
            InvitedBy = requesterUserId,
            Status = InvitationStatus.Pending
        };

        _invitations.Add(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // El grupo (con su nombre) ya está cargado; lo adjuntamos para armar la respuesta.
        invitation.Group = group;
        return InvitationResponse.FromEntity(invitation);
    }

    public async Task<IReadOnlyList<InvitationResponse>> GetMyInvitationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var invitations = await _invitations.GetPendingForUserAsync(userId, cancellationToken);
        return invitations.Select(InvitationResponse.FromEntity).ToList();
    }

    public async Task<InvitationResponse> AcceptAsync(Guid userId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await LoadOwnPendingInvitationAsync(userId, invitationId, cancellationToken);

        // Alta como miembro (si por alguna razón ya lo es, no se duplica).
        if (!await _groups.IsMemberAsync(invitation.GroupId, userId, cancellationToken))
        {
            _groups.AddMember(new GroupMember
            {
                GroupId = invitation.GroupId,
                UserId = userId,
                Role = GroupRole.Member
            });
        }

        invitation.Status = InvitationStatus.Accepted;
        invitation.RespondedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InvitationResponse.FromEntity(invitation);
    }

    public async Task<InvitationResponse> RejectAsync(Guid userId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await LoadOwnPendingInvitationAsync(userId, invitationId, cancellationToken);

        invitation.Status = InvitationStatus.Rejected;
        invitation.RespondedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InvitationResponse.FromEntity(invitation);
    }

    private async Task<GroupInvitation> LoadOwnPendingInvitationAsync(Guid userId, Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await _invitations.GetByIdAsync(invitationId, cancellationToken)
            ?? throw new NotFoundException("Invitación no encontrada.");

        if (invitation.InvitedUser != userId)
        {
            throw new ForbiddenException("Esta invitación no es para ti.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new ConflictException("La invitación ya fue respondida.");
        }

        return invitation;
    }
}
