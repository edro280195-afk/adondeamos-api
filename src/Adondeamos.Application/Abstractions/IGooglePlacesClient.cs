using Adondeamos.Application.DTOs.Places;

namespace Adondeamos.Application.Abstractions;

/// <summary>Cliente de Google Places API (New). Respeta los términos: búsqueda con Autocomplete y detalles bajo demanda.</summary>
public interface IGooglePlacesClient
{
    /// <summary>Autocomplete (sesiones gratis): devuelve predicciones para que el usuario elija.</summary>
    Task<IReadOnlyList<PlacePrediction>> AutocompleteAsync(string query, string? sessionToken, CancellationToken cancellationToken = default);

    /// <summary>Detalles bajo demanda de un place_id. Devuelve null si Google no lo encuentra.</summary>
    Task<GooglePlaceDetails?> GetDetailsAsync(string placeId, string? sessionToken, CancellationToken cancellationToken = default);
}
