using Adondeamos.Application.Services;
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

        // Servicios de negocio (uno por módulo).
        services.AddScoped<AuthService>();
        services.AddScoped<GroupService>();
        services.AddScoped<PlaceService>();
        services.AddScoped<SaveService>();
        services.AddScoped<ListService>();
        services.AddScoped<DecisionService>();

        return services;
    }
}
