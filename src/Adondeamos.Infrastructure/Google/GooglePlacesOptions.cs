namespace Adondeamos.Infrastructure.Google;

/// <summary>Opciones de Google Places, enlazadas desde la sección "GooglePlaces".</summary>
public sealed class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    public string BaseUrl { get; set; } = "https://places.googleapis.com/v1/";

    /// <summary>API key de Google. Va por user-secrets/variable de entorno; nunca hardcodeada.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
