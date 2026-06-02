namespace Adondeamos.Application.Common.Options;

/// <summary>
/// Opciones generales de la aplicación.
/// Sección "App" en la configuración.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// URL base usada para construir el link de confirmación de email.
    /// Ej: "https://api.adondeamos.app" o "http://localhost:5172" en dev.
    /// El link final será: {ConfirmEmailUrlBase}/auth/confirm-email?token={token}
    /// </summary>
    public string ConfirmEmailUrlBase { get; set; } = "http://localhost:5172";
}
