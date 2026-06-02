namespace Adondeamos.Application.DTOs.Places;

/// <summary>Resuelve un lugar de Google a partir de su place_id (el sessionToken es opcional, para facturación de Google).</summary>
public sealed record ResolvePlaceRequest(string GooglePlaceId, string? SessionToken);
