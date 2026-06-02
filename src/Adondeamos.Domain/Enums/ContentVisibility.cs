namespace Adondeamos.Domain.Enums;

/// <summary>
/// Visibilidad de un contenido (guardado o lista). Calca el tipo PostgreSQL <c>content_visibility</c>.
/// </summary>
public enum ContentVisibility
{
    /// <summary>Privado: solo el dueño.</summary>
    Private,

    /// <summary>Compartido con los grupos del usuario.</summary>
    Group,

    /// <summary>Público.</summary>
    Public
}
