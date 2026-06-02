using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Enums;
using Adondeamos.Infrastructure.Persistence;
using Adondeamos.Infrastructure.Repositories;
using Adondeamos.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.NameTranslation;

namespace Adondeamos.Infrastructure;

/// <summary>Registro de la capa Infrastructure: EF Core (Neon), seguridad, repositorios y Google Places.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:Default' (base de Neon). " +
                "Configúrala por user-secrets en dev o por variable de entorno en prod.");
        }

        // Mapea los enums nativos de PostgreSQL. Las etiquetas SQL se derivan de los nombres de los
        // miembros C# con el traductor snake_case (ej. GoogleMaps -> 'google_maps').
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var translator = new NpgsqlSnakeCaseNameTranslator();
        dataSourceBuilder.MapEnum<PlaceOrigin>("place_origin", translator);
        dataSourceBuilder.MapEnum<SocialNetwork>("social_network", translator);
        dataSourceBuilder.MapEnum<SaveStatus>("save_status", translator);
        dataSourceBuilder.MapEnum<ContentVisibility>("content_visibility", translator);
        dataSourceBuilder.MapEnum<GroupRole>("group_role", translator);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddDbContext<AdondeamosDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AdondeamosDbContext>());

        // Seguridad
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
