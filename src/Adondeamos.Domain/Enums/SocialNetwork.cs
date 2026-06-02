namespace Adondeamos.Domain.Enums;

/// <summary>
/// Red social de donde proviene un guardado. Calca el tipo PostgreSQL <c>social_network</c>.
/// </summary>
public enum SocialNetwork
{
    Tiktok,
    Instagram,
    Facebook,
    Whatsapp,

    /// <summary>Se traduce a la etiqueta <c>google_maps</c>.</summary>
    GoogleMaps,
    Youtube,

    /// <summary>Captura manual, sin red de origen.</summary>
    Manual
}
