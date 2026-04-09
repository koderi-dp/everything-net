using Everything.Net.Enums;
using Everything.Net.Models;
using Xunit;

namespace Everything.Net.Tests;

public sealed class EverythingQueryTests
{
    [Fact]
    public void Default_CreatesExpectedQuery()
    {
        var query = EverythingQuery.Default("invoice dm:today");

        Assert.Equal("invoice dm:today", query.SearchText);
        Assert.Equal((uint)0, query.Offset);
        Assert.Equal((uint)0, query.MaxResults);
        Assert.Null(query.Sort);
        Assert.True(query.WaitForResults);
        Assert.False(query.MatchPath);
        Assert.False(query.MatchCase);
        Assert.False(query.MatchWholeWord);
        Assert.False(query.Regex);
        Assert.Equal(
            EverythingRequestFlags.FileName | EverythingRequestFlags.Path,
            query.RequestFlags);
    }
}
