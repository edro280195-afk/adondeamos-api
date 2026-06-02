namespace Adondeamos.Domain.Enums;

/// <summary>
/// Rol de un usuario dentro de un grupo. Calca el tipo PostgreSQL <c>group_role</c>.
/// </summary>
public enum GroupRole
{
    /// <summary>Creador del grupo.</summary>
    Owner,

    /// <summary>Miembro del grupo.</summary>
    Member
}
