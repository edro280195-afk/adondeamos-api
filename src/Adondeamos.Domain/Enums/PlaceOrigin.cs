namespace Adondeamos.Domain.Enums;

/// <summary>
/// Origen de un lugar. Calca el tipo PostgreSQL <c>place_origin</c>.
/// Los nombres se traducen a snake_case para casar con las etiquetas del enum en la base.
/// </summary>
public enum PlaceOrigin
{
    /// <summary>Lugar del catálogo de Google (solo se guarda el google_place_id).</summary>
    Google,

    /// <summary>Lugar propio dado de alta por un usuario.</summary>
    Own
}
