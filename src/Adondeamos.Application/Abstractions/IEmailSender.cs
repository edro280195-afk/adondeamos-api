namespace Adondeamos.Application.Abstractions;

/// <summary>Abstracción de envío de correo. La implementación real o de dev se inyecta por configuración.</summary>
public interface IEmailSender
{
    /// <summary>Envía el correo con el link de confirmación de cuenta.</summary>
    Task SendConfirmationAsync(string toEmail, string userName, string confirmationLink, CancellationToken cancellationToken = default);
}
