using Adondeamos.Application.Abstractions;
using Adondeamos.Application.Common.Options;
using Adondeamos.Domain.Enums;
using Adondeamos.Infrastructure.Email;
using Adondeamos.Infrastructure.Google;
using Adondeamos.Infrastructure.Http;
using Adondeamos.Infrastructure.Persistence;
using Adondeamos.Infrastructure.Repositories;
using Adondeamos.Infrastructure.Security;
using Adondeamos.Infrastructure.Storage;
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
        dataSourceBuilder.MapEnum<InvitationStatus>("invitation_status", translator);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddDbContext<AdondeamosDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>(), npgsqlOptions =>
                {
                    npgsqlOptions.MapEnum<PlaceOrigin>("place_origin", nameTranslator: translator);
                    npgsqlOptions.MapEnum<SocialNetwork>("social_network", nameTranslator: translator);
                    npgsqlOptions.MapEnum<SaveStatus>("save_status", nameTranslator: translator);
                    npgsqlOptions.MapEnum<ContentVisibility>("content_visibility", nameTranslator: translator);
                    npgsqlOptions.MapEnum<GroupRole>("group_role", nameTranslator: translator);
                    npgsqlOptions.MapEnum<InvitationStatus>("invitation_status", nameTranslator: translator);
                })
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AdondeamosDbContext>());

        // Seguridad
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<ISaveRepository, SaveRepository>();
        services.AddScoped<IListRepository, ListRepository>();
        services.AddScoped<IDecisionRepository, DecisionRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();

        // Opciones de auth y app (usadas por los servicios de Application)
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        // Envío de correo: SMTP real si hay credenciales; DevEmailSender si no (solo loguea el link).
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        var smtpOptions = configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
        if (smtpOptions.IsConfigured)
        {
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddTransient<IEmailSender, DevEmailSender>();
        }

        // Cliente de Google Places (Places API New)
        services.AddGooglePlaces(configuration);

        // Almacenamiento de fotos: S3-compatible si hay credenciales; local (dev) si no.
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        if (storageOptions.S3.IsConfigured)
        {
            services.AddSingleton<IPhotoStorage, S3PhotoStorage>();
        }
        else
        {
            services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();
        }

        // Resolver de redirects (para enlaces cortos de Maps).
        services.AddHttpClient<ILinkResolver, LinkResolver>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
