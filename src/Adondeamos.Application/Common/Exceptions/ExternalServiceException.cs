namespace Adondeamos.Application.Common.Exceptions;

/// <summary>Falla al consultar un servicio externo (ej. Google Places). Se traduce a 502.</summary>
public sealed class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
