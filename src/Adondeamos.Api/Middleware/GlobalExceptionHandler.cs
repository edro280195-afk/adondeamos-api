using Adondeamos.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Adondeamos.Api.Middleware;

/// <summary>
/// Manejo centralizado de errores. Traduce las excepciones de la aplicación a respuestas
/// ProblemDetails con el código HTTP correcto. Nunca filtra detalles internos en un 500.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Datos inválidos"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autorizado"),
            ExternalServiceException => (StatusCodes.Status502BadGateway, "Servicio externo no disponible"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado");
        }
        else
        {
            _logger.LogWarning("Solicitud rechazada ({Status}): {Message}", status, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Ocurrió un error inesperado. Inténtalo de nuevo más tarde."
                : exception.Message
        };

        // En errores de validación adjunta el detalle por campo.
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());

            problemDetails.Extensions["errors"] = errors;
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
