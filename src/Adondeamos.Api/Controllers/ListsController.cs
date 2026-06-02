using Adondeamos.Api.Extensions;
using Adondeamos.Application.DTOs.Lists;
using Adondeamos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Route("lists")]
[Authorize]
public sealed class ListsController : ControllerBase
{
    private readonly ListService _listService;

    public ListsController(ListService listService) => _listService = listService;

    /// <summary>Crea una lista (personal si no se manda group_id; de grupo si se manda).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ListResponse>> Create(CreateListRequest request, CancellationToken cancellationToken)
    {
        var list = await _listService.CreateListAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = list.Id }, list);
    }

    /// <summary>Lista las listas del usuario (personales y de sus grupos).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ListResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var lists = await _listService.GetListsAsync(User.GetUserId(), cancellationToken);
        return Ok(lists);
    }

    /// <summary>Detalle de una lista con sus elementos.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ListDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var list = await _listService.GetListAsync(User.GetUserId(), id, cancellationToken);
        return Ok(list);
    }

    /// <summary>Agrega un guardado del usuario a la lista.</summary>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(ListItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ListItemResponse>> AddItem(Guid id, AddListItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _listService.AddItemAsync(User.GetUserId(), id, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, item);
    }

    /// <summary>Quita un guardado de la lista.</summary>
    [HttpDelete("{id:guid}/items/{saveId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid saveId, CancellationToken cancellationToken)
    {
        await _listService.RemoveItemAsync(User.GetUserId(), id, saveId, cancellationToken);
        return NoContent();
    }
}
