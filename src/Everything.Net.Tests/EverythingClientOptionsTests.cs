using Everything.Net.Configuration;
using Xunit;

namespace Everything.Net.Tests;

public sealed class EverythingClientOptionsTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var options = new EverythingClientOptions();

        Assert.True(options.ThrowOnUnavailableClient);
        Assert.Equal((uint)100, options.DefaultMaxResults);
        Assert.Equal((uint)0, options.DefaultOffset);
        Assert.True(options.RequestPathAndFileNameByDefault);
    }
}
