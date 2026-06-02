using Adondeamos.Domain.Enums;

namespace Adondeamos.Domain.Entities;

/// <summary>Pertenencia de un usuario a un grupo. Tabla <c>group_members</c> (PK compuesta).</summary>
public class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }

    public GroupRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
