using Adondeamos.Api.Extensions;
using Adondeamos.Application.DTOs.Groups;
using Adondeamos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Route("groups")]
[Authorize]
public sealed class GroupsController : ControllerBase
{
    private readonly GroupService _groupService;

    public GroupsController(GroupService groupService) => _groupService = groupService;

    /// <summary>Crea un grupo; el usuario autenticado queda como owner.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GroupDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupDetailResponse>> Create(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var group = await _groupService.CreateGroupAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    /// <summary>Lista los grupos a los que pertenece el usuario.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var groups = await _groupService.GetGroupsAsync(User.GetUserId(), cancellationToken);
        return Ok(groups);
    }

    /// <summary>Detalle de un grupo con sus miembros.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GroupDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var group = await _groupService.GetGroupAsync(User.GetUserId(), id, cancellationToken);
        return Ok(group);
    }
}
