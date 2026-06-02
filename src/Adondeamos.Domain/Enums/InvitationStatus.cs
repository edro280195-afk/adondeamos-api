namespace Adondeamos.Domain.Enums;

/// <summary>
/// Estado de una invitación a grupo. Calca el tipo PostgreSQL <c>invitation_status</c> (db/002).
/// </summary>
public enum InvitationStatus
{
    /// <summary>Pendiente de respuesta.</summary>
    Pending,

    /// <summary>Aceptada: el usuario entró al grupo.</summary>
    Accepted,

    /// <summary>Rechazada.</summary>
    Rejected
}
