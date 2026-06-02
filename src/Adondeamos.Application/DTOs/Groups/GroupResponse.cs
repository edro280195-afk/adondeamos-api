using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Groups;

/// <summary>Grupo en el listado del usuario, con el rol que tiene en él.</summary>
public sealed record GroupResponse(Guid Id, string Name, GroupRole Role, DateTime CreatedAt)
{
    public static GroupResponse FromMembership(GroupMember membership) =>
        new(membership.Group.Id, membership.Group.Name, membership.Role, membership.Group.CreatedAt);
}
