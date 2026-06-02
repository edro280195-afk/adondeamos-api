using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Entities;
using Adondeamos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adondeamos.Infrastructure.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AdondeamosDbContext _context;

    public EmailVerificationTokenRepository(AdondeamosDbContext context) => _context = context;

    public void Add(EmailVerificationToken token) => _context.EmailVerificationTokens.Add(token);

    public Task<EmailVerificationToken?> GetValidByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.EmailVerificationTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.ConsumedAt == null && t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task ConsumeAllPendingForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Consume en bulk los tokens no usados del usuario (limpieza antes de generar uno nuevo).
        var pending = await _context.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in pending)
        {
            token.ConsumedAt = now;
        }
    }
}
