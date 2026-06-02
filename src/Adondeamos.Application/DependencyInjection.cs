using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Adondeamos.Application;

/// <summary>Registro de la capa Application: validadores y servicios de negocio.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registra todos los validadores de FluentValidation de este ensamblado.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Servicios de negocio (se registran al construir cada módulo).
        return services;
    }
}
