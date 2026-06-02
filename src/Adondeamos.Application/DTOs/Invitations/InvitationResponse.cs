using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Invitations;

/// <summary>Invitación a un grupo.</summary>
public sealed record InvitationResponse(
    Guid Id,
    Guid GroupId,
    string GroupName,
    Guid InvitedUser,
    Guid? InvitedBy,
    InvitationStatus Status,
    DateTime CreatedAt,
    DateTime? RespondedAt)
{
    public static InvitationResponse FromEntity(GroupInvitation invitation) =>
        new(
            invitation.Id,
            invitation.GroupId,
            invitation.Group.Name,
            invitation.InvitedUser,
            invitation.InvitedBy,
            invitation.Status,
            invitation.CreatedAt,
            invitation.RespondedAt);
}
