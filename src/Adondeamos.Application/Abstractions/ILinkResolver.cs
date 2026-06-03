namespace Adondeamos.Application.Abstractions;

/// <summary>
/// Resuelve la URL final de un enlace siguiendo redirects HTTP.
/// Se usa para acortar URLs como maps.app.goo.gl antes de extraer datos.
/// </summary>
public interface ILinkResolver
{
    /// <summary>
    /// Sigue todos los redirects de <paramref name="url"/> y devuelve la URL final.
    /// Si ocurre cualquier error de red, devuelve la URL original.
    /// </summary>
    Task<string> FollowRedirectsAsync(string url, CancellationToken cancellationToken = default);
}
