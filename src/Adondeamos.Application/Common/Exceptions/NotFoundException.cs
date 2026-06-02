namespace Adondeamos.Application.Common.Exceptions;

/// <summary>El recurso solicitado no existe. Se traduce a 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
