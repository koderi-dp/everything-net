using Everything.Net.Abstractions;
using Everything.Net.Configuration;
using Everything.Net.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Everything.Net.DependencyInjection;

/// <summary>
/// Extension methods for registering Everything services with dependency injection.
/// </summary>
public static class EverythingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Everything client and default options.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddEverythingClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<EverythingClientOptions>();
        services.AddSingleton<IEverythingClient, EverythingClient>();

        return services;
    }

    /// <summary>
    /// Registers the Everything client and configures its options.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">The delegate used to configure client options.</param>
    /// <returns>The same service collection for chaining.</returns>
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
