using Adondeamos.Domain.Enums;

namespace Adondeamos.Application.DTOs.Places;

/// <summary>
/// Resultado de intentar resolver un enlace.
/// — Resolved=true: la URL era de Google Maps y se pudo obtener el lugar.
/// — Resolved=false: la URL no es de Maps o no se pudo extraer el place_id;
///   el cliente debe mostrar el flujo de búsqueda/manual con la URL ya adjunta.
/// </summary>
public sealed record ResolveLinkResponse
{
    public bool Resolved { get; init; }

    /// <summary>Lugar resuelto. Solo presente cuando <see cref="Resolved"/> es true.</summary>
    public ResolvePlaceResponse? Place { get; init; }

    /// <summary>Red social detectada. Solo presente cuando <see cref="Resolved"/> es false.</summary>
    public SocialNetwork? SourceNetwork { get; init; }

    /// <summary>URL original. Solo presente cuando <see cref="Resolved"/> es false.</summary>
    public string? Url { get; init; }
}
