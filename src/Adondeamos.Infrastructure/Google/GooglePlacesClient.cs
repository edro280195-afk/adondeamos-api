using System.Net;
using System.Net.Http.Json;
using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Places;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adondeamos.Infrastructure.Google;

/// <summary>
/// Cliente de Google Places API (New). Usa Autocomplete para buscar (gratis) y trae detalles
/// bajo demanda con field masks (control de costo). La API key viaja en el header X-Goog-Api-Key.
/// </summary>
public sealed class GooglePlacesClient : IGooglePlacesClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GooglePlacesClient> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public GooglePlacesClient(HttpClient httpClient, IOptions<GooglePlacesOptions> options, ILogger<GooglePlacesClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var value = options.Value;
        _baseUrl = value.BaseUrl.EndsWith('/') ? value.BaseUrl : value.BaseUrl + "/";
        _apiKey = value.ApiKey;
    }

    public async Task<IReadOnlyList<PlacePrediction>> AutocompleteAsync(string query, string? sessionToken, CancellationToken cancellationToken = default)
    {
        EnsureApiKey();

        var body = new Dictionary<string, object?>
        {
            ["input"] = query,
            ["languageCode"] = "es",
            ["regionCode"] = "MX"
        };
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            body["sessionToken"] = sessionToken;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}places:autocomplete")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-Goog-Api-Key", _apiKey);
        request.Headers.Add("X-Goog-FieldMask",
            "suggestions.placePrediction.placeId,suggestions.placePrediction.text,suggestions.placePrediction.structuredFormat");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await LogAndThrowAsync(response, "autocomplete", cancellationToken);
        }

        var payload = await response.Content.ReadFromJsonAsync<AutocompleteResponse>(cancellationToken);
        if (payload?.Suggestions is null)
        {
            return [];
        }

        return payload.Suggestions
            .Where(s => s.PlacePrediction is { PlaceId: not null })
            .Select(s => new PlacePrediction(
                s.PlacePrediction!.PlaceId!,
                s.PlacePrediction.Text?.Text ?? string.Empty,
                s.PlacePrediction.StructuredFormat?.MainText?.Text,
                s.PlacePrediction.StructuredFormat?.SecondaryText?.Text))
            .ToList();
    }

    public async Task<GooglePlaceDetails?> GetDetailsAsync(string placeId, string? sessionToken, CancellationToken cancellationToken = default)
    {
        EnsureApiKey();

        var url = $"{_baseUrl}places/{Uri.EscapeDataString(placeId)}";
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            url += $"?sessionToken={Uri.EscapeDataString(sessionToken)}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Goog-Api-Key", _apiKey);
        // photos se trae bajo demanda junto con los datos del lugar (control de costo con field mask).
        request.Headers.Add("X-Goog-FieldMask", "id,displayName,formattedAddress,location,photos");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            await LogAndThrowAsync(response, "detalles del lugar", cancellationToken);
        }

        var details = await response.Content.ReadFromJsonAsync<PlaceDetailsResponse>(cancellationToken);
        if (details?.Id is null)
        {
            return null;
        }

        // Resuelve la primera foto disponible (no se guarda; muéstrase con atribución).
        string? photoUrl = null;
        string? photoAttribution = null;
        var firstPhoto = details.Photos?.FirstOrDefault();
        if (firstPhoto?.Name is not null)
        {
            (photoUrl, photoAttribution) = await ResolvePhotoUrlAsync(firstPhoto, cancellationToken);
        }

        return new GooglePlaceDetails(
            details.Id,
            details.DisplayName?.Text,
            details.FormattedAddress,
            details.Location is null ? null : (decimal)details.Location.Latitude,
            details.Location is null ? null : (decimal)details.Location.Longitude,
            photoUrl,
            photoAttribution);
    }

    /// <summary>
    /// Llama a /{photo.Name}/media con skipHttpRedirect=true para obtener la URL pública
    /// de la CDN de Google sin seguir un redirect. Devuelve (null, null) si falla silenciosamente.
    /// </summary>
    private async Task<(string? url, string? attribution)> ResolvePhotoUrlAsync(PhotoDto photo, CancellationToken cancellationToken)
    {
        try
        {
            // photo.Name ya incluye el prefijo "places/..." → URL completa con base.
            var mediaUrl = $"{_baseUrl}{photo.Name}/media?maxWidthPx=800&skipHttpRedirect=true";
            using var request = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
            request.Headers.Add("X-Goog-Api-Key", _apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (null, null);
            }

            var media = await response.Content.ReadFromJsonAsync<PhotoMediaResponse>(cancellationToken);
            var attribution = photo.AuthorAttributions?.FirstOrDefault()?.DisplayName;
            return (media?.PhotoUri, attribution);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Google Photos] No se pudo obtener la foto del lugar.");
            return (null, null);
        }
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ExternalServiceException("El servicio de lugares (Google Places) no está configurado.");
        }
    }

    private async Task LogAndThrowAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Google Places falló en {Operation}: {Status} {Body}", operation, (int)response.StatusCode, detail);
        throw new ExternalServiceException("No se pudo consultar Google Places en este momento.");
    }

    // --- Modelos internos de deserialización (respuestas de Places API New) ---

    private sealed class AutocompleteResponse
    {
        public List<Suggestion>? Suggestions { get; set; }
    }

    private sealed class Suggestion
    {
        public PlacePredictionDto? PlacePrediction { get; set; }
    }

    private sealed class PlacePredictionDto
    {
        public string? PlaceId { get; set; }
        public TextValue? Text { get; set; }
        public StructuredFormat? StructuredFormat { get; set; }
    }

    private sealed class StructuredFormat
    {
        public TextValue? MainText { get; set; }
        public TextValue? SecondaryText { get; set; }
    }

    private sealed class TextValue
    {
        public string? Text { get; set; }
    }

    private sealed class PlaceDetailsResponse
    {
        public string? Id { get; set; }
        public TextValue? DisplayName { get; set; }
        public string? FormattedAddress { get; set; }
        public LatLng? Location { get; set; }
        public List<PhotoDto>? Photos { get; set; }
    }

    private sealed class LatLng
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    private sealed class PhotoDto
    {
        /// <summary>Resource name en el formato places/{id}/photos/{ref}. Sirve como ruta para /media.</summary>
        public string? Name { get; set; }
        public List<AuthorAttribution>? AuthorAttributions { get; set; }
    }

    private sealed class AuthorAttribution
    {
        public string? DisplayName { get; set; }
        public string? Uri { get; set; }
    }

    private sealed class PhotoMediaResponse
    {
        /// <summary>URL pública de la foto en la CDN de Google (lh3.googleusercontent.com).</summary>
        public string? PhotoUri { get; set; }
    }
}
