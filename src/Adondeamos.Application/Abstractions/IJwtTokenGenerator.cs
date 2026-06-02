using Adondeamos.Domain.Entities;

namespace Adondeamos.Application.Abstractions;

/// <summary>Genera el token JWT de acceso para un usuario.</summary>
public interface IJwtTokenGenerator
{
    TokenResult GenerateToken(User user);
}

/// <summary>Token generado y el instante (UTC) en que expira.</summary>
public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);
