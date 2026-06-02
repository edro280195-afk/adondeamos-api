namespace Adondeamos.Application.Common.Validation;

/// <summary>Reglas de validación reutilizables.</summary>
public static class ValidationHelpers
{
    /// <summary>True si la cadena es una URL absoluta http/https.</summary>
    public static bool BeAValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
