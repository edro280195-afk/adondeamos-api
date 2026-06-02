using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.Abstractions;

public interface IEmailVerificationTokenRepository
{
    void Add(EmailVerificationToken token);

    /// <summary>Busca un token válido (no expirado, no consumido) por su hash SHA-256.</summary>
    Task<EmailVerificationToken?> GetValidByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Invalida todos los tokens pendientes anteriores del mismo usuario (limpieza al re-enviar).</summary>
    Task ConsumeAllPendingForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
