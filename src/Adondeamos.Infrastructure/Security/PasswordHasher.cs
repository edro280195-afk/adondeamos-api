using Adondeamos.Application.Abstractions;

namespace Adondeamos.Infrastructure.Security;

/// <summary>Hash de contraseñas con BCrypt.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
