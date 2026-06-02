namespace Adondeamos.Infrastructure.Email;

/// <summary>Credenciales y parámetros del servidor SMTP. Sección "Email:Smtp".</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;

    /// <summary>True para usar STARTTLS (port 587); false para SSL implícito (port 465).</summary>
    public bool UseTls { get; set; } = true;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Dirección del remitente. Ej: "Adondeamos &lt;noreply@adondeamos.app&gt;"</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>True si el servidor SMTP está configurado (host + credenciales presentes).</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FromAddress);
}
