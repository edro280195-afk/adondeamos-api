namespace Adondeamos.Application.Common.Exceptions;

/// <summary>La operación choca con el estado actual (ej. email duplicado). Se traduce a 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
