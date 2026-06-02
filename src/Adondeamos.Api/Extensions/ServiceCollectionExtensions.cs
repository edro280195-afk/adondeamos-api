using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adondeamos.Api.Middleware;
using Adondeamos.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Adondeamos.Api.Extensions;

/// <summary>Registro de los servicios de la capa web: controllers, errores, CORS, JWT y Swagger.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Nombre de la política de CORS para el frontend.</summary>
    public const string CorsPolicyName = "AdondeamosCors";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Los enums viajan como texto camelCase (ej. "tiktok", "googleMaps") en vez de números.
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        services.AddEndpointsApiExplorer();

        // Errores centralizados -> ProblemDetails.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Health check de liveness (no toca la base; Render lo usa para evitar cold starts).
        services.AddHealthChecks();

        AddCorsPolicy(services, configuration);
        AddJwtAuthentication(services, configuration);
        AddSwagger(services);

        return services;
    }

    private static void AddCorsPolicy(IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
                else
                {
                    // Sin orígenes configurados (típico en dev): se permite cualquiera para facilitar pruebas.
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Conserva el nombre original de los claims (ej. "sub") sin remapear a URIs largas.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Adondeamos API",
                Version = "v1",
                Description = "Núcleo V1: auth, grupos, lugares, guardados, listas y decisiones."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Pega tu token así: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // En Microsoft.OpenApi 2.x la referencia al esquema se hace con OpenApiSecuritySchemeReference
            // y el requisito se registra como factory que recibe el documento anfitrión.
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer", document, null), new List<string>() }
            });
        });
    }
}
