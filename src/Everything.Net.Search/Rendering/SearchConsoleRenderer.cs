using Everything.Net.Models;
using Everything.Net.Search.Models;
using Everything.Net.Search.Services;

namespace Everything.Net.Search.Rendering;

internal static class SearchConsoleRenderer
{
    public static void RenderHeader()
    {
        Console.WriteLine("Everything");
        Console.WriteLine("Interactive file and folder search powered by Everything.Net");
        Console.WriteLine(new string('=', 72));
    }

    public static void RenderResults(EverythingQueryResponse response, SearchCliOptions options)
    {
        var filteredResults = Filter(response.Results, options.ResultType).ToList();

        Console.WriteLine($"Query:   {response.SearchText}");
        Console.WriteLine($"Visible: {filteredResults.Count}");
        Console.WriteLine($"Matched: {response.TotalResults}");
        Console.WriteLine($"Type:    {FormatResultType(options.ResultType)}");
        Console.WriteLine($"Sort:    {SearchSortParser.Display(options.Sort)}");
        Console.WriteLine();

        if (filteredResults.Count == 0)
        {
            Console.WriteLine("No results matched the current query/filter.");
            return;
        }

        foreach (var (item, index) in filteredResults.Select((item, index) => (item, index)))
        {
            var prefix = item.IsFolder ? "[d]" : "[f]";
            var line = $"{options.Offset + (uint)index + 1,4} {prefix} {item.FullPath}";

            if (options.ShowDetails)
            {
                line += $"  size={FormatSize(item.Size),8}  modified={FormatDate(item.DateModified)}";
            }

            Console.WriteLine(line);
        }
    }

    public static void RenderHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Everything.Net.Search <query>");
        Console.WriteLine("  -n, --limit <number>      Maximum number of results to request. Default: 25.");
        Console.WriteLine("  --offset <number>         Skip this many results before returning items.");
        Console.WriteLine("  --sort <name>             Sort by modified, name, path, size, extension, created, accessed.");
        Console.WriteLine("  --files                   Show files only.");
        Console.WriteLine("  --folders                 Show folders only.");
        Console.WriteLine("  --details                 Include size and modified time columns.");
        Console.WriteLine("  --no-details              Hide extra columns.");
        Console.WriteLine("  --regex                   Treat the query as a regex.");
        Console.WriteLine("  --match-case              Enable case-sensitive matching.");
        Console.WriteLine("  --whole-word              Require whole-word matching.");
        Console.WriteLine("  --match-path              Include path text in matching.");
        Console.WriteLine("  --no-prompt               Disable interactive mode when no query is passed.");
        Console.WriteLine("  -h, --help                Show help.");
    }

    public static void RenderUnavailable()
    {
        Console.WriteLine("Everything is not available.");
        Console.WriteLine("Make sure Everything is installed, running, and the native SDK DLLs are present.");
    }

    public static void RenderSearchFailure(EverythingQueryResponse response)
    {
        Console.WriteLine($"Search failed: {response.ErrorMessage ?? response.ErrorCode.ToString()}");
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

    private static string FormatResultType(SearchResultType type) => type switch
    {
        SearchResultType.Files => "files",
        SearchResultType.Folders => "folders",
        _ => "all"
    };

    private static string FormatDate(DateTimeOffset? value) => value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";

    private static string FormatSize(long? bytes)
    {
        if (bytes is null)
        {
            return "-";
        }

        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes.Value;
        var order = 0;

        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {suffixes[order]}";
    }
}
