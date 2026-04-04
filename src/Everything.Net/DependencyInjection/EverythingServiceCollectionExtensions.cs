using Everything.Net.Abstractions;
using Everything.Net.Configuration;
using Everything.Net.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Everything.Net.DependencyInjection;

public static class EverythingServiceCollectionExtensions
{
    public static IServiceCollection AddEverythingClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<EverythingClientOptions>();
        services.AddSingleton<IEverythingClient, EverythingClient>();

        return services;
    }

    public static IServiceCollection AddEverythingClient(
        this IServiceCollection services,
        Action<EverythingClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<EverythingClientOptions>().Configure(configure);
        services.AddSingleton<IEverythingClient, EverythingClient>();

        return services;
    }
}
