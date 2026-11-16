using Everything.Net.Enums;
using Everything.Net.Internal;
using Everything.Net.Models;
using Xunit;
using Fake = Everything.Net.Tests.EverythingClientTests.FakeEverythingNativeAdapter;

namespace Everything.Net.Tests;

public sealed class SdkSelectionTests
{
    [Fact]
    public void Auto_UsesSdk3_WhenItConnects()
    {
        var sdk2 = new Fake { SdkVersion = EverythingSdkVersion.V2 };
        var sdk3 = new Fake { SdkVersion = EverythingSdkVersion.V3 };
        var auto = new AutoNativeAdapter(sdk2, sdk3, () => true);

        auto.SetSearch("abc");

        Assert.Equal(EverythingSdkVersion.V3, auto.SdkVersion);
        Assert.Equal("abc", sdk3.SearchText);
        Assert.Equal(string.Empty, sdk2.SearchText);
    }

    [Fact]
    public void Auto_FallsBackToSdk2_WhenSdk3CannotConnect_AndTriesAgainNextTime()
    {
        var connected = false;
        var sdk2 = new Fake { SdkVersion = EverythingSdkVersion.V2 };
        var sdk3 = new Fake { SdkVersion = EverythingSdkVersion.V3 };
        var auto = new AutoNativeAdapter(sdk2, sdk3, () => connected);

        auto.SetSearch("first");
        Assert.Equal(EverythingSdkVersion.V2, auto.SdkVersion);

        connected = true;
        auto.SetSearch("second");
        Assert.Equal(EverythingSdkVersion.V3, auto.SdkVersion);
        Assert.Equal("first", sdk2.SearchText);
        Assert.Equal("second", sdk3.SearchText);
    }

    [Fact]
    public void Auto_StopsTryingSdk3_WhenItsDllIsMissing()
    {
        var attempts = 0;
        var sdk2 = new Fake { SdkVersion = EverythingSdkVersion.V2 };
        var sdk3 = new Fake { SdkVersion = EverythingSdkVersion.V3 };
        var auto = new AutoNativeAdapter(sdk2, sdk3, () =>
        {
            attempts++;
            throw new DllNotFoundException();
        });

        auto.SetSearch("first");
        auto.SetSearch("second");

        Assert.Equal(EverythingSdkVersion.V2, auto.SdkVersion);
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(EverythingSort.NameAscending, 0u, true)]
    [InlineData(EverythingSort.NameDescending, 0u, false)]
    [InlineData(EverythingSort.PathAscending, 1u, true)]
    [InlineData(EverythingSort.SizeDescending, 2u, false)]
    [InlineData(EverythingSort.ExtensionAscending, 3u, true)]
    [InlineData(EverythingSort.TypeNameDescending, 4u, false)]
    [InlineData(EverythingSort.DateCreatedAscending, 6u, true)]
    [InlineData(EverythingSort.DateModifiedDescending, 5u, false)]
    [InlineData(EverythingSort.AttributesAscending, 8u, true)]
    [InlineData(EverythingSort.RunCountDescending, 10u, false)]
    [InlineData(EverythingSort.DateRecentlyChangedAscending, 9u, true)]
    [InlineData(EverythingSort.DateAccessedDescending, 7u, false)]
    [InlineData(EverythingSort.DateRunAscending, 11u, true)]
    public void Sdk3_MapsSdk2SortValues(EverythingSort sort, uint expectedProperty, bool expectedAscending)
    {
        Assert.True(Everything3NativeAdapter.TryMapSort((uint)sort, out var property, out var ascending));
        Assert.Equal(expectedProperty, property);
        Assert.Equal(expectedAscending, ascending);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(27u)]
    public void Sdk3_RejectsUnknownSortValues(uint sort)
    {
        Assert.False(Everything3NativeAdapter.TryMapSort(sort, out _, out _));
    }
}
