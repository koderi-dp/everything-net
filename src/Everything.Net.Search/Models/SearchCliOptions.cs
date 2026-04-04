using Everything.Net.Enums;
using Everything.Net.Search.Services;

namespace Everything.Net.Search.Models;

internal sealed record SearchCliOptions(
    string QueryText,
    uint Limit,
    uint Offset,
    EverythingSort? Sort,
    SearchResultType ResultType,
    bool ShowDetails,
    bool Regex,
    bool MatchCase,
    bool MatchWholeWord,
    bool MatchPath,
    bool NoPrompt,
    bool ShowHelp)
{
    public static SearchCliOptions Default => new(
        QueryText: string.Empty,
        Limit: 25,
        Offset: 0,
        Sort: EverythingSort.DateModifiedDescending,
        ResultType: SearchResultType.All,
        ShowDetails: true,
        Regex: false,
        MatchCase: false,
        MatchWholeWord: false,
        MatchPath: false,
        NoPrompt: false,
        ShowHelp: false);

    public static SearchCliOptions Parse(string[] args)
    {
        var options = Default;
        var queryParts = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-h":
                case "--help":
                    options = options with { ShowHelp = true };
                    break;
                case "-n":
                case "--limit":
                    options = options with { Limit = ParseUInt(args, ref i, arg) };
                    break;
                case "--offset":
                    options = options with { Offset = ParseUInt(args, ref i, arg) };
                    break;
                case "--sort":
                    options = options with { Sort = SearchSortParser.Parse(ParseString(args, ref i, arg)) };
                    break;
                case "--files":
                    EnsureTypeNotAlreadySelected(options.ResultType, SearchResultType.Files, arg);
                    options = options with { ResultType = SearchResultType.Files };
                    break;
                case "--folders":
                    EnsureTypeNotAlreadySelected(options.ResultType, SearchResultType.Folders, arg);
                    options = options with { ResultType = SearchResultType.Folders };
                    break;
                case "--details":
                    options = options with { ShowDetails = true };
                    break;
                case "--no-details":
                    options = options with { ShowDetails = false };
                    break;
                case "--regex":
                    options = options with { Regex = true };
                    break;
                case "--match-case":
                    options = options with { MatchCase = true };
                    break;
                case "--whole-word":
                    options = options with { MatchWholeWord = true };
                    break;
                case "--match-path":
                    options = options with { MatchPath = true };
                    break;
                case "--no-prompt":
                    options = options with { NoPrompt = true };
                    break;
                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown option '{arg}'.");
                    }

                    queryParts.Add(arg);
                    break;
            }
        }

        return options with { QueryText = string.Join(' ', queryParts) };
    }

    private static uint ParseUInt(IReadOnlyList<string> args, ref int index, string optionName)
    {
        var value = ParseString(args, ref index, optionName);
        if (!uint.TryParse(value, out var parsed))
        {
            throw new ArgumentException($"Option '{optionName}' expects an unsigned integer.");
        }

        return parsed;
    }

    private static string ParseString(IReadOnlyList<string> args, ref int index, string optionName)
    {
        index++;
        if (index >= args.Count)
        {
            throw new ArgumentException($"Option '{optionName}' expects a value.");
        }

        return args[index];
    }

    private static void EnsureTypeNotAlreadySelected(
        SearchResultType current,
        SearchResultType next,
        string optionName)
    {
        if (current != SearchResultType.All && current != next)
        {
            throw new ArgumentException($"Option '{optionName}' conflicts with the already selected result type.");
        }
    }
}
