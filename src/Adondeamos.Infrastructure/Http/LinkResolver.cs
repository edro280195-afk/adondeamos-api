using Adondeamos.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Adondeamos.Infrastructure.Http;

/// <summary>
/// Sigue los redirects de una URL con un HttpClient configurado con AllowAutoRedirect=true.
/// La URL final se lee de RequestMessage.RequestUri del último response.
/// Se usa principalmente para acortar URLs de Google Maps (maps.app.goo.gl → URL completa).
/// </summary>
public sealed class LinkResolver : ILinkResolver
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkResolver> _logger;

    public LinkResolver(HttpClient httpClient, ILogger<LinkResolver> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> FollowRedirectsAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            // HEAD evita descargar el body; solo necesitamos la URL final tras los redirects.
            using var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            _logger.LogDebug("[LinkResolver] {Original} → {Final}", url, finalUrl);
            return finalUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LinkResolver] No se pudo seguir el redirect de {Url}.", url);
            return url;
        }
    }
}
