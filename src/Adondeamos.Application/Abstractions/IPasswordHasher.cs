namespace Adondeamos.Application.Abstractions;

/// <summary>Hashea y verifica contraseñas.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
