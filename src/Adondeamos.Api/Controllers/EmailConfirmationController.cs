using Adondeamos.Application.DTOs.Auth;
using Adondeamos.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class EmailConfirmationController : ControllerBase
{
    private readonly EmailConfirmationService _confirmationService;

    public EmailConfirmationController(EmailConfirmationService confirmationService)
        => _confirmationService = confirmationService;

    /// <summary>
    /// Confirma el correo del usuario con el token recibido por email.
    /// En modo AutoConfirmEmail (dev) la cuenta ya está confirmada al registrar;
    /// este endpoint igualmente consume el token si se llama.
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        await _confirmationService.ConfirmEmailAsync(request, cancellationToken);
        return Ok(new { message = "Correo confirmado correctamente." });
    }

    /// <summary>
    /// Reenvía el correo de confirmación. Responde siempre 200 sin revelar si el email existe
    /// (evita enumeración de usuarios).
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        await _confirmationService.ResendAsync(request, cancellationToken);
        return Ok(new { message = "Si el correo existe y no está confirmado, recibirás el link en breve." });
    }
}
