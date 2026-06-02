namespace Adondeamos.Infrastructure.Security;

/// <summary>Opciones del JWT, enlazadas desde la sección "Jwt" de la configuración.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Clave secreta para firmar (HS256). Debe tener al menos 32 caracteres. Va por user-secrets/variable de entorno.</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "adondeamos";
    public string Audience { get; set; } = "adondeamos";

    /// <summary>Vigencia del token en minutos. Por defecto 7 días.</summary>
    public int ExpirationMinutes { get; set; } = 60 * 24 * 7;
}
