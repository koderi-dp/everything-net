using Everything.Net.Abstractions;
using Everything.Net.Configuration;
using Everything.Net.DependencyInjection;
using Everything.Net.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Everything.Net.Tests;

public sealed class EverythingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEverythingClient_RegistersClientAndDefaultOptions()
    {
        var services = new ServiceCollection();

        services.AddEverythingClient();

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IEverythingClient>();
        var options = provider.GetRequiredService<IOptions<EverythingClientOptions>>().Value;

        Assert.IsType<EverythingClient>(client);
        Assert.True(options.ThrowOnUnavailableClient);
        Assert.Equal((uint)100, options.DefaultMaxResults);
        Assert.Equal((uint)0, options.DefaultOffset);
        Assert.True(options.RequestPathAndFileNameByDefault);
    }

    [Fact]
    public void AddEverythingClient_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddEverythingClient(options =>
        {
            options.ThrowOnUnavailableClient = false;
            options.DefaultMaxResults = 250;
            options.DefaultOffset = 10;
            options.RequestPathAndFileNameByDefault = false;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EverythingClientOptions>>().Value;

        Assert.False(options.ThrowOnUnavailableClient);
        Assert.Equal((uint)250, options.DefaultMaxResults);
        Assert.Equal((uint)10, options.DefaultOffset);
        Assert.False(options.RequestPathAndFileNameByDefault);
    }

    [Fact]
    public void AddEverythingClient_ThrowsForNullServices()
    {
        ServiceCollection? services = null;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services!.AddEverythingClient());

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddEverythingClient_WithConfigure_ThrowsForNullConfigureDelegate()
    {
        var services = new ServiceCollection();
        Action<EverythingClientOptions>? configure = null;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddEverythingClient(configure!));

        Assert.Equal("configure", exception.ParamName);
    }
}
