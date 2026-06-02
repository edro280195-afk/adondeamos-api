namespace Adondeamos.Application.DTOs.Places;

/// <summary>
/// Detalles traídos de Google BAJO DEMANDA. No se persisten en nuestra base.
/// Deben mostrarse con la atribución de Google.
/// </summary>
public sealed record GooglePlaceDetails(
    string PlaceId,
    string? DisplayName,
    string? FormattedAddress,
    decimal? Latitude,
    decimal? Longitude);
