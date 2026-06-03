namespace Adondeamos.Application.DTOs.Places;

/// <summary>
/// Enlace a resolver. Puede ser de Google Maps (cualquier variante, incluso acortada)
/// o de otra red social. Si no es de Maps, se devuelve la red detectada para que el
/// cliente inicie el flujo manual.
/// </summary>
public sealed record ResolveLinkRequest(string Url);
