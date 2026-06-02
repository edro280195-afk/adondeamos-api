using Adondeamos.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Adondeamos.Infrastructure.Email;

/// <summary>
/// Implementación de <see cref="IEmailSender"/> para desarrollo y pruebas.
/// NO manda correos reales: escribe el link de confirmación en el log estructurado.
/// Se usa cuando no hay credenciales SMTP configuradas.
/// </summary>
public sealed class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger) => _logger = logger;

    public Task SendConfirmationAsync(
        string toEmail,
        string userName,
        string confirmationLink,
        CancellationToken cancellationToken = default)
    {
        // El link aparece en el log con nivel Warning para que sea fácil de encontrar en la consola.
        _logger.LogWarning(
            "[DEV] Correo de confirmación para {UserName} <{Email}>. " +
            "Link (cópialo en el navegador o en Swagger): {ConfirmationLink}",
            userName, toEmail, confirmationLink);

        return Task.CompletedTask;
    }
}
