using System.Text.RegularExpressions;
using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Exceptions;
using Adondeamos.Application.DTOs.Places;
using Adondeamos.Domain.Entities;
using Adondeamos.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Adondeamos.Application.Services;

/// <summary>Lugares: búsqueda (Autocomplete), resolución de lugares de Google y alta de lugares propios.</summary>
public sealed class PlaceService
{
    private readonly IPlaceRepository _places;
    private readonly IGooglePlacesClient _google;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ResolvePlaceRequest> _resolveValidator;
    private readonly IValidator<CreateOwnPlaceRequest> _createOwnValidator;
    private readonly ILinkResolver _linkResolver;
    private readonly ILogger<PlaceService> _logger;

    public PlaceService(
        IPlaceRepository places,
        IGooglePlacesClient google,
        IUnitOfWork unitOfWork,
        IValidator<ResolvePlaceRequest> resolveValidator,
        IValidator<CreateOwnPlaceRequest> createOwnValidator,
        ILinkResolver linkResolver,
        ILogger<PlaceService> logger)
    {
        _places = places;
        _google = google;
        _unitOfWork = unitOfWork;
        _resolveValidator = resolveValidator;
        _createOwnValidator = createOwnValidator;
        _linkResolver = linkResolver;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlacePrediction>> SearchAsync(string? query, string? sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ValidationException("Debes indicar el término de búsqueda 'q'.");
        }

        return await _google.AutocompleteAsync(query.Trim(), sessionToken, cancellationToken);
    }

    public async Task<ResolvePlaceResponse> ResolveAsync(Guid userId, ResolvePlaceRequest request, CancellationToken cancellationToken = default)
    {
        await _resolveValidator.ValidateAndThrowAsync(request, cancellationToken);

        var googlePlaceId = request.GooglePlaceId.Trim();

        // Detalles bajo demanda: además de mostrarse, validan que el place_id exista en Google.
        var details = await _google.GetDetailsAsync(googlePlaceId, request.SessionToken, cancellationToken)
            ?? throw new NotFoundException("No se encontró el lugar en Google.");

        // Canoniza por place_id: si ya existe, se devuelve el mismo registro (no se duplica).
        var existing = await _places.GetByGooglePlaceIdAsync(googlePlaceId, cancellationToken);

        Place place;
        if (existing is not null)
        {
            place = existing;
        }
        else
        {
            var created = new Place
            {
                Origin = PlaceOrigin.Google,
                GooglePlaceId = googlePlaceId,
                // Guardamos el nombre para mostrarlo en listas sin necesitar otra llamada a Google.
                Name = details.DisplayName,
                CreatedBy = userId
            };
            _places.Add(created);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                place = created;
            }
            catch (ConflictException)
            {
                // Otra petición lo insertó al mismo tiempo: recupera el existente y sigue.
                place = await _places.GetByGooglePlaceIdAsync(googlePlaceId, cancellationToken)
                    ?? throw new ConflictException("No se pudo resolver el lugar de Google.");
            }
        }

        return new ResolvePlaceResponse(PlaceResponse.FromEntity(place), details);
    }

    public async Task<PlaceResponse> CreateOwnAsync(Guid userId, CreateOwnPlaceRequest request, CancellationToken cancellationToken = default)
    {
        await _createOwnValidator.ValidateAndThrowAsync(request, cancellationToken);

        var place = new Place
        {
            Origin = PlaceOrigin.Own,
            Name = request.Name.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            CreatedBy = userId
        };

        _places.Add(place);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PlaceResponse.FromEntity(place);
    }

    /// <summary>
    /// Intenta resolver un enlace: si es de Google Maps extrae el lugar; si no, devuelve la
    /// red detectada para que el cliente inicie el flujo de búsqueda/manual.
    /// </summary>
    public async Task<ResolveLinkResponse> ResolveLinkAsync(
        Guid userId, ResolveLinkRequest request, CancellationToken cancellationToken = default)
    {
        var rawUrl = request.Url?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            throw new ValidationException("La URL no puede estar vacía.");
        }

        var network = DetectSocialNetwork(rawUrl);

        if (network != SocialNetwork.GoogleMaps)
        {
            return new ResolveLinkResponse { Resolved = false, SourceNetwork = network, Url = rawUrl };
        }

        // Para URLs cortas (maps.app.goo.gl, goo.gl/maps) seguimos los redirects primero.
        var urlToAnalyze = IsShortMapsUrl(rawUrl)
            ? await _linkResolver.FollowRedirectsAsync(rawUrl, cancellationToken)
            : rawUrl;

        var placeId = ExtractGooglePlaceId(urlToAnalyze);
        if (placeId is null)
        {
            _logger.LogWarning("[ResolveLink] No se pudo extraer place_id de: {Url}", urlToAnalyze);
            return new ResolveLinkResponse
            {
                Resolved = false,
                SourceNetwork = SocialNetwork.GoogleMaps,
                Url = rawUrl
            };
        }

        var resolveRequest = new ResolvePlaceRequest(placeId, null);
        var resolved = await ResolveAsync(userId, resolveRequest, cancellationToken);
        return new ResolveLinkResponse { Resolved = true, Place = resolved };
    }

    // --- Helpers de detección y extracción ---

    private static SocialNetwork DetectSocialNetwork(string url)
    {
        if (url.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("vm.tiktok.com", StringComparison.OrdinalIgnoreCase))
            return SocialNetwork.Tiktok;

        if (url.Contains("instagram.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("instagr.am", StringComparison.OrdinalIgnoreCase))
            return SocialNetwork.Instagram;

        if (url.Contains("facebook.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("fb.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("fb.watch", StringComparison.OrdinalIgnoreCase))
            return SocialNetwork.Facebook;

        if (url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            return SocialNetwork.Youtube;

        if (url.Contains("whatsapp.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("wa.me", StringComparison.OrdinalIgnoreCase))
            return SocialNetwork.Whatsapp;

        if (IsGoogleMapsUrl(url))
            return SocialNetwork.GoogleMaps;

        return SocialNetwork.Manual;
    }

    private static bool IsGoogleMapsUrl(string url) =>
        url.Contains("maps.app.goo.gl", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("goo.gl/maps", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("google.com/maps", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("maps.google.com", StringComparison.OrdinalIgnoreCase);

    private static bool IsShortMapsUrl(string url) =>
        url.Contains("maps.app.goo.gl", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("goo.gl/maps", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Intenta extraer el place_id de Google de los patrones más comunes de URLs de Maps.
    /// Soporta: ?place_id=ChIJ…, q=place_id:ChIJ…, y !1sChIJ… en el parámetro data=.
    /// </summary>
    private static string? ExtractGooglePlaceId(string url)
    {
        // Pattern 1: ?place_id=ChIJ... o &place_id=ChIJ...
        var m = Regex.Match(url, @"[?&]place_id=([^&#]+)");
        if (m.Success)
            return Uri.UnescapeDataString(m.Groups[1].Value);

        // Pattern 2: q=place_id:ChIJ...
        m = Regex.Match(url, @"[?&]q=place_id:([^&#]+)", RegexOptions.IgnoreCase);
        if (m.Success)
            return Uri.UnescapeDataString(m.Groups[1].Value);

        // Pattern 3: !1sChIJ... en el parámetro data= de URLs desktop de Google Maps
        m = Regex.Match(url, @"!1s(ChIJ[^!]+)");
        if (m.Success)
            return Uri.UnescapeDataString(m.Groups[1].Value);

        return null;
    }
}
