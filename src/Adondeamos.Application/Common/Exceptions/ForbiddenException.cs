namespace Adondeamos.Application.Common.Exceptions;

/// <summary>El usuario autenticado no tiene permiso sobre el recurso. Se traduce a 403.</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
