namespace Adondeamos.Application.DTOs.Places;

/// <summary>
/// Resultado de resolver un lugar de Google: el registro canónico de nuestra base más los
/// detalles de Google traídos bajo demanda (estos NO se guardan; muéstralos con atribución a Google).
/// </summary>
public sealed record ResolvePlaceResponse(PlaceResponse Place, GooglePlaceDetails Google);
