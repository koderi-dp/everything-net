using Everything.Net.Internal;
using Xunit;

namespace Everything.Net.Tests;

public sealed class EverythingNativeLibraryLocatorTests
{
    private const string Dll = "Everything64.dll";

    [Fact]
    public void GetCandidates_TriesConfiguredPath_ThenInstalledCopy_ThenBundledCopy()
    {
        var candidates = EverythingNativeLibraryLocator.GetCandidates(
            Dll, "win-x64", @"D:\tools\sdk", @"C:\Program Files\Everything", @"C:\app").ToArray();

        Assert.Equal(
        [
            @"D:\tools\sdk\Everything64.dll",
            @"C:\Program Files\Everything\sdk\dll\Everything64.dll",
            @"C:\Program Files\Everything\Everything64.dll",
            @"C:\app\runtimes\win-x64\native\Everything64.dll",
            @"C:\app\Everything64.dll"
        ], candidates);
    }

    [Theory]
    [InlineData(@"D:\tools\sdk\Everything64.dll", "Everything64.dll", @"D:\tools\sdk\Everything64.dll")]
    [InlineData(@"D:\tools\sdk\Everything64.dll", "Everything3_x64.dll", @"D:\tools\sdk\Everything3_x64.dll")]
    [InlineData(@"D:\tools\sdk", "Everything3_x64.dll", @"D:\tools\sdk\Everything3_x64.dll")]
    public void GetCandidates_NeverLoadsOneSdkDllInPlaceOfTheOther(string configured, string wanted, string expectedFirst)
    {
        var first = EverythingNativeLibraryLocator.GetCandidates(wanted, "win-x64", configured, null, @"C:\app").First();

        Assert.Equal(expectedFirst, first);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void GetCandidates_FallsBackToBundledCopy_WhenNothingElseIsKnown(string? missing)
    {
        var candidates = EverythingNativeLibraryLocator.GetCandidates(Dll, "win-arm64", missing, missing, @"C:\app").ToArray();

        Assert.Equal(
        [
            @"C:\app\runtimes\win-arm64\native\Everything64.dll",
            @"C:\app\Everything64.dll"
        ], candidates);
    }
}
