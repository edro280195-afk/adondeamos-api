using Adondeamos.Api.Extensions;
using Adondeamos.Application.DTOs.Places;
using Adondeamos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Route("places")]
[Authorize]
public sealed class PlacesController : ControllerBase
{
    private readonly PlaceService _placeService;

    public PlacesController(PlaceService placeService) => _placeService = placeService;

    /// <summary>Busca lugares con Autocomplete de Google (gratis). Devuelve predicciones para elegir.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<PlacePrediction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<PlacePrediction>>> Search(
        [FromQuery] string q,
        [FromQuery] string? sessionToken,
        CancellationToken cancellationToken)
    {
        var predictions = await _placeService.SearchAsync(q, sessionToken, cancellationToken);
        return Ok(predictions);
    }

    /// <summary>
    /// Resuelve un lugar de Google: trae sus detalles bajo demanda y crea (o devuelve) el registro
    /// canónico. Solo se persiste el google_place_id; los detalles de Google se muestran con atribución.
    /// </summary>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(ResolvePlaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ResolvePlaceResponse>> Resolve(ResolvePlaceRequest request, CancellationToken cancellationToken)
    {
        var result = await _placeService.ResolveAsync(User.GetUserId(), request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Crea un lugar propio (origin='own') cuando no está en Google.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlaceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaceResponse>> CreateOwn(CreateOwnPlaceRequest request, CancellationToken cancellationToken)
    {
        var place = await _placeService.CreateOwnAsync(User.GetUserId(), request, cancellationToken);
        return Created($"/places/{place.Id}", place);
    }

    /// <summary>
    /// Intenta resolver un enlace (Google Maps, TikTok, Instagram, etc.).
    /// — Si es de Maps: resuelve el lugar y lo devuelve (resolved=true).
    /// — Si no es de Maps: devuelve la red detectada y la URL para que el cliente
    ///   inicie el flujo de búsqueda/manual (resolved=false).
    /// </summary>
    [HttpPost("resolve-link")]
    [ProducesResponseType(typeof(ResolveLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ResolveLinkResponse>> ResolveLink(
        ResolveLinkRequest request, CancellationToken cancellationToken)
    {
        var result = await _placeService.ResolveLinkAsync(User.GetUserId(), request, cancellationToken);
        return Ok(result);
    }
}
