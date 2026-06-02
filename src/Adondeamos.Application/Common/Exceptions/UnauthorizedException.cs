namespace Adondeamos.Application.Common.Exceptions;

/// <summary>Credenciales inválidas o ausentes. Se traduce a 401.</summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
