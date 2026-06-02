using Adondeamos.Api.Extensions;
using Adondeamos.Application.DTOs.Auth;
using Adondeamos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly AuthService _authService;

    public MeController(AuthService authService) => _authService = authService;

    /// <summary>Perfil del usuario autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await _authService.GetProfileAsync(User.GetUserId(), cancellationToken);
        return Ok(profile);
    }

    /// <summary>Actualiza el nombre y/o el avatar del usuario autenticado.</summary>
    [HttpPatch]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UpdateProfile(UpdateMeRequest request, CancellationToken cancellationToken)
    {
        var profile = await _authService.UpdateProfileAsync(User.GetUserId(), request, cancellationToken);
        return Ok(profile);
    }
}
