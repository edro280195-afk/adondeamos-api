namespace Adondeamos.Application.DTOs.Places;

/// <summary>
/// Detalles traídos de Google BAJO DEMANDA. No se persisten en nuestra base.
/// Deben mostrarse con la atribución correspondiente de Google.
/// </summary>
public sealed record GooglePlaceDetails(
    string PlaceId,
    string? DisplayName,
    string? FormattedAddress,
    decimal? Latitude,
    decimal? Longitude,
    /// <summary>URL pública de la primera foto del lugar. No se guarda; muéstrala con <see cref="PhotoAttribution"/>.</summary>
    string? PhotoUrl,
    /// <summary>Nombre del autor de la foto (Google Maps Contributor).</summary>
    string? PhotoAttribution);
