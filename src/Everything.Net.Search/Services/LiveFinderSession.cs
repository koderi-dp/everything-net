using System.Text;
using Everything.Net.Models;
using Everything.Net.Search.Models;
using Everything.Net.Search.Rendering;
using Everything.Net.Services;
using Spectre.Console;

namespace Everything.Net.Search.Services;

internal sealed class LiveFinderSession(EverythingClient client)
{
    private readonly FinderPreviewService _previewService = new();

    public async Task<int> RunAsync(SearchCliOptions initialOptions)
    {
        var options = initialOptions with { Offset = 0, ShowDetails = false };
        var state = new FinderViewState(
            options,
            null,
            null,
            false,
            0,
            FinderPreviewContent.Empty("Preview", "Start typing to search."));
        var buffer = new StringBuilder(options.QueryText);
        var pendingSearch = !string.IsNullOrWhiteSpace(options.QueryText);
        var originalTreatControlCAsInput = Console.TreatControlCAsInput;

        Console.TreatControlCAsInput = true;

        try
        {
            await AnsiConsole.Live(SearchConsoleRenderer.RenderLiveFinder(state))
                .Overflow(VerticalOverflow.Ellipsis)
                .Cropping(VerticalOverflowCropping.Bottom)
                .AutoClear(false)
                .StartAsync(async context =>
                {
                    while (true)
                    {
                        if (pendingSearch)
                        {
                            state = await SearchAsync(state with
                            {
                                Options = state.Options with { QueryText = buffer.ToString() }
                            });
                            pendingSearch = false;
                        }
                        else
                        {
                            state = state with { Options = state.Options with { QueryText = buffer.ToString() } };
                        }

                        context.UpdateTarget(SearchConsoleRenderer.RenderLiveFinder(state));

                        var key = Console.ReadKey(intercept: true);
                        var action = HandleKey(key, state.Options, buffer);

                        if (action.ExitRequested)
                        {
                            return;
                        }

                        options = action.Options;
                        state = state with
                        {
                            Options = options with { QueryText = buffer.ToString() },
                            ErrorMessage = action.ErrorMessage,
                            ShowHelp = action.ToggleHelpRequested ? !state.ShowHelp : state.ShowHelp,
                            SelectedIndex = action.SelectedIndexDelta.HasValue
                                ? state.SelectedIndex + action.SelectedIndexDelta.Value
                                : state.SelectedIndex
                        };

                        if (action.ResetSelection)
                        {
                            state = state with { SelectedIndex = 0 };
                        }

                        if (state.SelectedResult is null)
                        {
                            state = state with { SelectedIndex = 0 };
                        }
                        else if (state.SelectedIndex >= state.VisibleResults.Count)
                        {
                            state = state with { SelectedIndex = state.VisibleResults.Count - 1 };
                        }

                        state = state with { Preview = _previewService.Build(state.SelectedResult) };

                        pendingSearch = action.TriggerSearch;
                    }
                });
        }
        finally
        {
            Console.TreatControlCAsInput = originalTreatControlCAsInput;
            AnsiConsole.Clear();
            SearchConsoleRenderer.RenderHeader();
        }

        return 0;
    }

    private async Task<FinderViewState> SearchAsync(FinderViewState state)
    {
        if (string.IsNullOrWhiteSpace(state.Options.QueryText))
        {
            return state with
            {
                Response = null,
                ErrorMessage = null,
                SelectedIndex = 0,
                Preview = FinderPreviewContent.Empty("Preview", "Start typing to search.")
            };
        }

        var response = await ExecuteSearchAsync(state.Options);

        if (!response.Success)
        {
            return state with
            {
                Response = null,
                ErrorMessage = response.ErrorMessage ?? response.ErrorCode.ToString(),
                SelectedIndex = 0,
                Preview = FinderPreviewContent.Empty("Preview", response.ErrorMessage ?? response.ErrorCode.ToString())
            };
        }

        var visibleResults = FilterVisibleResults(response.Results, state.Options.ResultType).ToList();
        var nextIndex = Math.Clamp(state.SelectedIndex, 0, Math.Max(0, visibleResults.Count - 1));
        var selected = visibleResults.Count == 0 ? null : visibleResults[nextIndex];

        return state with
        {
            Response = response,
            ErrorMessage = null,
            SelectedIndex = nextIndex,
            Preview = _previewService.Build(selected)
        };
    }

    private async Task<EverythingQueryResponse> ExecuteSearchAsync(SearchCliOptions options)
    {
        var query = SearchQueryFactory.Build(options);

        try
        {
            return await client.SearchAsync(query);
        }
        catch (Exception ex)
        {
            return new EverythingQueryResponse
            {
                SearchText = options.QueryText,
                Success = false,
                TotalResults = 0,
                Results = Array.Empty<EverythingSearchResult>(),
                ErrorCode = 0,
                ErrorMessage = ex.Message
            };
        }
    }

    private static LiveFinderAction HandleKey(ConsoleKeyInfo key, SearchCliOptions options, StringBuilder query)
    {
        if (!char.IsControl(key.KeyChar) && key.Key != ConsoleKey.Backspace)
        {
            query.Append(key.KeyChar);
            return LiveFinderAction.Search(options with { Offset = 0 }, resetSelection: true);
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return HandleControlKey(key, options, query);
        }

        return key.Key switch
        {
            ConsoleKey.Backspace => HandleBackspace(options, query),
            ConsoleKey.Enter => LiveFinderAction.Search(options with { Offset = 0 }, resetSelection: false),
            ConsoleKey.Escape => LiveFinderAction.Exit(options),
            ConsoleKey.UpArrow => LiveFinderAction.MoveSelection(options, selectedIndexDelta: -1),
            ConsoleKey.DownArrow => LiveFinderAction.MoveSelection(options, selectedIndexDelta: 1),
            ConsoleKey.PageDown => LiveFinderAction.Search(options with { Offset = options.Offset + options.Limit }, resetSelection: true),
            ConsoleKey.PageUp => LiveFinderAction.Search(options with { Offset = options.Offset > options.Limit ? options.Offset - options.Limit : 0 }, resetSelection: true),
            ConsoleKey.Tab => LiveFinderAction.Search(options with
            {
                ResultType = NextResultType(options.ResultType),
                Offset = 0
            }, resetSelection: true),
            ConsoleKey.F1 => LiveFinderAction.ToggleHelp(options),
            ConsoleKey.F2 => LiveFinderAction.Search(options with { ShowDetails = !options.ShowDetails }, resetSelection: false),
            ConsoleKey.F3 => LiveFinderAction.Search(options with
            {
                Sort = SearchSortParser.Next(options.Sort),
                Offset = 0
            }, resetSelection: true),
            ConsoleKey.F4 => LiveFinderAction.Search(options with { Regex = !options.Regex, Offset = 0 }, resetSelection: true),
            ConsoleKey.F5 => LiveFinderAction.Search(options, resetSelection: false),
            _ => LiveFinderAction.None(options)
        };
    }

    private static LiveFinderAction HandleControlKey(ConsoleKeyInfo key, SearchCliOptions options, StringBuilder query)
    {
        return key.Key switch
        {
            ConsoleKey.L => ClearQuery(options, query),
            ConsoleKey.F => LiveFinderAction.Search(options with { ResultType = SearchResultType.Files, Offset = 0 }, resetSelection: true),
            ConsoleKey.D => LiveFinderAction.Search(options with { ShowDetails = !options.ShowDetails }, resetSelection: false),
            ConsoleKey.R => LiveFinderAction.Search(options with { Regex = !options.Regex, Offset = 0 }, resetSelection: true),
            ConsoleKey.P => LiveFinderAction.Search(options with { MatchPath = !options.MatchPath, Offset = 0 }, resetSelection: true),
            ConsoleKey.W => LiveFinderAction.Search(options with { MatchWholeWord = !options.MatchWholeWord, Offset = 0 }, resetSelection: true),
            ConsoleKey.C => LiveFinderAction.Search(options with { MatchCase = !options.MatchCase, Offset = 0 }, resetSelection: true),
            ConsoleKey.A => LiveFinderAction.Search(options with { ResultType = SearchResultType.All, Offset = 0 }, resetSelection: true),
            ConsoleKey.O => LiveFinderAction.Search(options with { ResultType = SearchResultType.Folders, Offset = 0 }, resetSelection: true),
            ConsoleKey.H => LiveFinderAction.ToggleHelp(options),
            ConsoleKey.Q => LiveFinderAction.Exit(options),
            _ => LiveFinderAction.None(options)
        };
    }

    private static LiveFinderAction HandleBackspace(SearchCliOptions options, StringBuilder query)
    {
        if (query.Length == 0)
        {
            return LiveFinderAction.None(options);
        }

        query.Length--;
        return LiveFinderAction.Search(options with { Offset = 0 }, resetSelection: true);
    }

    private static LiveFinderAction ClearQuery(SearchCliOptions options, StringBuilder query)
    {
        query.Clear();
        return LiveFinderAction.Search(options with { Offset = 0 }, resetSelection: true);
    }

    private static SearchResultType NextResultType(SearchResultType current)
    {
        return current switch
        {
            SearchResultType.All => SearchResultType.Files,
            SearchResultType.Files => SearchResultType.Folders,
            _ => SearchResultType.All
        };
    }

    private static IEnumerable<EverythingSearchResult> FilterVisibleResults(
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

    private sealed record LiveFinderAction(
        SearchCliOptions Options,
        bool TriggerSearch,
        bool ExitRequested,
        bool ResetSelection,
        bool ToggleHelpRequested,
        string? ErrorMessage,
        int? SelectedIndexDelta)
    {
        public static LiveFinderAction None(SearchCliOptions options) =>
            new(options, false, false, false, false, null, null);

        public static LiveFinderAction Search(SearchCliOptions options, bool resetSelection) =>
            new(options, true, false, resetSelection, false, null, null);

        public static LiveFinderAction Exit(SearchCliOptions options) =>
            new(options, false, true, false, false, null, null);

        public static LiveFinderAction ToggleHelp(SearchCliOptions options) =>
            new(options, false, false, false, true, null, null);

        public static LiveFinderAction MoveSelection(SearchCliOptions options, int selectedIndexDelta) =>
            new(options, false, false, false, false, null, selectedIndexDelta);
    }
}
