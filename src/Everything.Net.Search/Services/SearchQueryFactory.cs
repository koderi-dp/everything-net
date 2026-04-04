using Everything.Net.Enums;
using Everything.Net.Models;
using Everything.Net.Search.Models;

namespace Everything.Net.Search.Services;

internal static class SearchQueryFactory
{
    public static EverythingQuery Build(SearchCliOptions options)
    {
        var flags =
            EverythingRequestFlags.FileName |
            EverythingRequestFlags.Path |
            EverythingRequestFlags.Extension;

        if (options.ShowDetails)
        {
            flags |= EverythingRequestFlags.Size | EverythingRequestFlags.DateModified;
        }

        return new EverythingQuery
        {
            SearchText = options.QueryText,
            Offset = options.Offset,
            MaxResults = options.Limit,
            Sort = options.Sort,
            RequestFlags = flags,
            WaitForResults = true,
            MatchPath = options.MatchPath,
            MatchCase = options.MatchCase,
            MatchWholeWord = options.MatchWholeWord,
            Regex = options.Regex
        };
    }
}
