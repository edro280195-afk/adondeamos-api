using Adondeamos.Domain.Enums;

namespace Adondeamos.Domain.Entities;

/// <summary>
/// Invitación para unirse a un grupo. Tabla <c>group_invitations</c> (db/002).
/// El invitado debe aceptar para pasar a ser miembro (group_members).
/// </summary>
public class GroupInvitation
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }

    /// <summary>Usuario invitado.</summary>
    public Guid InvitedUser { get; set; }

    /// <summary>Quién envió la invitación.</summary>
    public Guid? InvitedBy { get; set; }

    public InvitationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Cuándo se aceptó o rechazó.</summary>
    public DateTime? RespondedAt { get; set; }

    public Group Group { get; set; } = null!;
}
