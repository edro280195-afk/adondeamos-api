using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.DTOs.Groups;

/// <summary>Detalle de un grupo con sus miembros.</summary>
public sealed record GroupDetailResponse(Guid Id, string Name, DateTime CreatedAt, IReadOnlyList<GroupMemberResponse> Members)
{
    public static GroupDetailResponse FromEntity(Group group) =>
        new(
            group.Id,
            group.Name,
            group.CreatedAt,
            group.Members
                .OrderBy(m => m.JoinedAt)
                .Select(GroupMemberResponse.FromEntity)
                .ToList());
}
