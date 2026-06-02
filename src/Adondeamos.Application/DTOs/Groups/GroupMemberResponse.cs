using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Groups;

/// <summary>Miembro de un grupo.</summary>
public sealed record GroupMemberResponse(Guid UserId, string Name, string Email, GroupRole Role, DateTime JoinedAt)
{
    public static GroupMemberResponse FromEntity(GroupMember member) =>
        new(member.UserId, member.User.Name, member.User.Email, member.Role, member.JoinedAt);
}
