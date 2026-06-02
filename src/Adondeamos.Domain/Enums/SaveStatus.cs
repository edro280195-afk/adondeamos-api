namespace Adondeamos.Domain.Enums;

/// <summary>
/// Estado de un guardado. Calca el tipo PostgreSQL <c>save_status</c>.
/// </summary>
public enum SaveStatus
{
    /// <summary>Pendiente por visitar.</summary>
    Pending,

    /// <summary>Ya visitado.</summary>
    Visited
}
