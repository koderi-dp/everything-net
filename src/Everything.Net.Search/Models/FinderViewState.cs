using Everything.Net.Models;

namespace Everything.Net.Search.Models;

internal sealed record FinderViewState(
    SearchCliOptions Options,
    EverythingQueryResponse? Response,
    string? ErrorMessage,
    bool ShowHelp,
    int SelectedIndex,
    int PreviewScrollOffset,
    FinderPreviewContent Preview)
{
    public IReadOnlyList<EverythingSearchResult> VisibleResults =>
        Response is null
            ? Array.Empty<EverythingSearchResult>()
            : Filter(Response.Results, Options.ResultType).ToList();

    public EverythingSearchResult? SelectedResult
    {
        get
        {
            var results = VisibleResults;
            if (results.Count == 0)
            {
                return null;
            }

            var index = Math.Clamp(SelectedIndex, 0, results.Count - 1);
            return results[index];
        }
    }

    private static IEnumerable<EverythingSearchResult> Filter(
        IReadOnlyList<EverythingSearchResult> results,
        SearchResultType resultType)
    {
        return resultType switch
        {
            SearchResultType.Files => results.Where(static item => item.IsFile),
            SearchResultType.Folders => results.Where(static item => item.IsFolder),
            _ => results
        };
    }
}
