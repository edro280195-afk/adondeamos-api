using Adondeamos.Api.Extensions;
using Adondeamos.Application.DTOs.Saves;
using Adondeamos.Application.Services;
using Adondeamos.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Route("saves")]
[Authorize]
public sealed class SavesController : ControllerBase
{
    private readonly SaveService _saveService;

    public SavesController(SaveService saveService) => _saveService = saveService;

    /// <summary>Guarda un lugar (ya resuelto) para el usuario autenticado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SaveResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaveResponse>> Create(CreateSaveRequest request, CancellationToken cancellationToken)
    {
        var save = await _saveService.CreateSaveAsync(User.GetUserId(), request, cancellationToken);
        return Created($"/saves/{save.Id}", save);
    }

    /// <summary>Lista los guardados del usuario, con filtros opcionales por estado y por lista.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SaveResponse>>> GetMine(
        [FromQuery] SaveStatus? status,
        [FromQuery] Guid? listId,
        CancellationToken cancellationToken)
    {
        var saves = await _saveService.GetSavesAsync(User.GetUserId(), status, listId, cancellationToken);
        return Ok(saves);
    }

    /// <summary>Actualiza un guardado (nota, visibilidad o marcarlo como visitado).</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(SaveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaveResponse>> Update(Guid id, UpdateSaveRequest request, CancellationToken cancellationToken)
    {
        var save = await _saveService.UpdateSaveAsync(User.GetUserId(), id, request, cancellationToken);
        return Ok(save);
    }

    /// <summary>Elimina un guardado del usuario.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _saveService.DeleteSaveAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
