using Adondeamos.Api.Extensions;
using Adondeamos.Application.DTOs.Decisions;
using Adondeamos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Route("decisions")]
[Authorize]
public sealed class DecisionsController : ControllerBase
{
    private readonly DecisionService _decisionService;

    public DecisionsController(DecisionService decisionService) => _decisionService = decisionService;

    /// <summary>Inicia una sesión de decisión (en solitario o de grupo).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DecisionDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DecisionDetailResponse>> Create(CreateDecisionRequest request, CancellationToken cancellationToken)
    {
        var decision = await _decisionService.CreateDecisionAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = decision.Id }, decision);
    }

    /// <summary>Agrega lugares candidatos (por place_ids y/o auto-llenando desde los guardados pendientes).</summary>
    [HttpPost("{id:guid}/options")]
    [ProducesResponseType(typeof(DecisionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionDetailResponse>> AddOptions(Guid id, AddOptionsRequest request, CancellationToken cancellationToken)
    {
        var decision = await _decisionService.AddOptionsAsync(User.GetUserId(), id, request, cancellationToken);
        return Ok(decision);
    }

    /// <summary>Registra el voto del usuario sobre una opción (true = sí, false = no).</summary>
    [HttpPost("{id:guid}/options/{optionId:guid}/votes")]
    [ProducesResponseType(typeof(DecisionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionDetailResponse>> Vote(Guid id, Guid optionId, CastVoteRequest request, CancellationToken cancellationToken)
    {
        var decision = await _decisionService.CastVoteAsync(User.GetUserId(), id, optionId, request, cancellationToken);
        return Ok(decision);
    }

    /// <summary>Estado de la sesión: opciones, votos y el match calculado.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DecisionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var decision = await _decisionService.GetDecisionAsync(User.GetUserId(), id, cancellationToken);
        return Ok(decision);
    }
}
