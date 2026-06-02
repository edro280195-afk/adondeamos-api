using Adondeamos.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using System.Net;

namespace Adondeamos.Infrastructure.Email;

/// <summary>
/// Implementación de <see cref="IEmailSender"/> con MailKit.
/// Admite cualquier proveedor SMTP (Gmail, Resend, Brevo, Postmark, etc.).
/// Se usa cuando las credenciales Email:Smtp:* están configuradas.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendConfirmationAsync(
        string toEmail,
        string userName,
        string confirmationLink,
        CancellationToken cancellationToken = default)
    {
        var message = BuildMessage(toEmail, userName, confirmationLink);

        using var client = new SmtpClient();
        try
        {
            // STARTTLS (587) o SSL implícito (465) según configuración.
            var socketOptions = _options.UseTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.SslOnConnect;

            await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation("Correo de confirmación enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al enviar correo de confirmación a {Email}", toEmail);
            throw;
        }
    }

    private MimeMessage BuildMessage(string toEmail, string userName, string confirmationLink)
    {
        var from = MailboxAddress.Parse(_options.FromAddress);
        var to = new MailboxAddress(userName, toEmail);

        var html = $"""
            <!DOCTYPE html>
            <html lang="es">
            <head><meta charset="utf-8"></head>
            <body style="font-family:sans-serif;max-width:600px;margin:0 auto;padding:24px">
              <h2>Confirma tu correo en Adondeamos</h2>
              <p>Hola <strong>{WebUtility.HtmlEncode(userName)}</strong>,</p>
              <p>Haz clic en el botón para confirmar tu cuenta:</p>
              <p>
                <a href="{WebUtility.HtmlEncode(confirmationLink)}"
                   style="display:inline-block;padding:12px 24px;background:#0ea5e9;color:#fff;
                          text-decoration:none;border-radius:6px;font-weight:bold">
                  Confirmar correo
                </a>
              </p>
              <p style="color:#666;font-size:0.875em">
                O copia este link en tu navegador:<br>
                <a href="{WebUtility.HtmlEncode(confirmationLink)}">{confirmationLink}</a>
              </p>
              <p style="color:#666;font-size:0.875em">
                El link expira en 48 horas. Si no creaste esta cuenta ignora este correo.
              </p>
            </body>
            </html>
            """;

        var message = new MimeMessage();
        message.From.Add(from);
        message.To.Add(to);
        message.Subject = "Confirma tu correo en Adondeamos";
        message.Body = new TextPart(TextFormat.Html) { Text = html };

        return message;
    }
}
