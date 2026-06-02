using Adondeamos.Api.Extensions;
using Adondeamos.Application.DTOs.Invitations;
using Adondeamos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Authorize]
public sealed class InvitationsController : ControllerBase
{
    private readonly InvitationService _invitationService;

    public InvitationsController(InvitationService invitationService) => _invitationService = invitationService;

    /// <summary>Invita a un usuario al grupo (por correo o id). Queda pendiente hasta que el invitado acepte.</summary>
    [HttpPost("/groups/{groupId:guid}/invitations")]
    [ProducesResponseType(typeof(InvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvitationResponse>> Invite(Guid groupId, InviteMemberRequest request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationService.InviteAsync(User.GetUserId(), groupId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, invitation);
    }

    /// <summary>Mis invitaciones pendientes.</summary>
    [HttpGet("/me/invitations")]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvitationResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var invitations = await _invitationService.GetMyInvitationsAsync(User.GetUserId(), cancellationToken);
        return Ok(invitations);
    }

    /// <summary>Acepta una invitación: el usuario entra al grupo.</summary>
    [HttpPost("/invitations/{id:guid}/accept")]
    [ProducesResponseType(typeof(InvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvitationResponse>> Accept(Guid id, CancellationToken cancellationToken)
    {
        var invitation = await _invitationService.AcceptAsync(User.GetUserId(), id, cancellationToken);
        return Ok(invitation);
    }

    /// <summary>Rechaza una invitación.</summary>
    [HttpPost("/invitations/{id:guid}/reject")]
    [ProducesResponseType(typeof(InvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvitationResponse>> Reject(Guid id, CancellationToken cancellationToken)
    {
        var invitation = await _invitationService.RejectAsync(User.GetUserId(), id, cancellationToken);
        return Ok(invitation);
    }
}
