namespace Adondeamos.Application.Common.Options;

/// <summary>
/// Opciones de comportamiento del módulo de autenticación.
/// Sección "Auth" en la configuración.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Si es true, el registro marca email_confirmed=true de inmediato, sin necesidad de clic.
    /// Recomendado en dev/pruebas. Default: true.
    /// </summary>
    public bool AutoConfirmEmail { get; set; } = true;

    /// <summary>
    /// Si es true, el login falla con 403 si email_confirmed=false.
    /// Default: false (permite login antes de confirmar).
    /// </summary>
    public bool RequireConfirmedEmailToLogin { get; set; } = false;

    /// <summary>
    /// Tiempo de vida del token de confirmación de email. Default: 48 horas.
    /// </summary>
    public int ConfirmationTokenExpirationHours { get; set; } = 48;
}
