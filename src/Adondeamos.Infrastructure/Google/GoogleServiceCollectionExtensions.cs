using Adondeamos.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adondeamos.Infrastructure.Google;

public static class GoogleServiceCollectionExtensions
{
    public static IServiceCollection AddGooglePlaces(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GooglePlacesOptions>(configuration.GetSection(GooglePlacesOptions.SectionName));

        services.AddHttpClient<IGooglePlacesClient, GooglePlacesClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
