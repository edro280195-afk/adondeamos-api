namespace Adondeamos.Domain.Entities;

/// <summary>
/// Token de un solo uso para verificar el correo de un usuario. Tabla <c>email_verification_tokens</c>.
/// Nunca se guarda el token en claro; solo su hash SHA-256 (<see cref="TokenHash"/>).
/// </summary>
public class EmailVerificationToken
{
    public Guid Id { get; set; }

    /// <summary>Usuario al que pertenece el token.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256 del token en claro. El token en claro viaja en el link de confirmación.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Nulo = disponible; con valor = ya usado (consumido).</summary>
    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
