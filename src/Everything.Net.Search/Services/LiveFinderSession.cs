using System.Diagnostics;
using System.Text;
using Everything.Net.Models;
using Everything.Net.Search.Models;
using Everything.Net.Search.Rendering;
using Everything.Net.Services;
using Spectre.Tui;

namespace Everything.Net.Search.Services;

internal sealed class LiveFinderSession(EverythingClient client)
{
    private readonly FinderPreviewService _previewService = new();
    private const int DefaultVisibleRows = 10;

    public async Task<int> RunAsync(SearchCliOptions initialOptions)
    {
        var options = initialOptions with { Offset = 0, ShowDetails = false, Limit = DefaultVisibleRows };
        var state = new FinderViewState(
            options,
            null,
            null,
            false,
            FinderPaneFocus.Results,
            0,
            0,
            0,
            FinderPreviewContent.Empty("Preview", "Start typing to search."));
        var buffer = new StringBuilder(options.QueryText);
        var pendingSearch = !string.IsNullOrWhiteSpace(options.QueryText);
        var originalTreatControlCAsInput = Console.TreatControlCAsInput;
        var finderRenderer = new FinderTuiRenderer();

        Console.TreatControlCAsInput = true;

        try
        {
            using var terminal = Terminal.Create();
            var renderer = new Renderer(terminal);
            renderer.SetTargetFps(30);

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

                state = ClampViewportState(state, finderRenderer.VisiblePreviewLines, finderRenderer.VisiblePreviewWidth);

                renderer.Draw((context, _) => finderRenderer.Render(context, state));

                state = ClampViewportState(state, finderRenderer.VisiblePreviewLines, finderRenderer.VisiblePreviewWidth);

                var desiredLimit = (uint)Math.Max(1, finderRenderer.VisibleResultRows);
                if (state.Options.Limit != desiredLimit)
                {
                    options = state.Options with { Limit = desiredLimit };
                    state = state with { Options = options };
                    pendingSearch = !string.IsNullOrWhiteSpace(buffer.ToString());
                    continue;
                }

                if (!Console.KeyAvailable)
                {
                    await Task.Delay(16);
                    continue;
                }

                var key = Console.ReadKey(intercept: true);
                var action = HandleKey(
                    key,
                    state,
                    buffer,
                    finderRenderer.VisiblePreviewLines);

                if (action.ExitRequested)
                {
                    break;
                }

                options = action.Options;
                var previousSelection = state.SelectedIndex;
                var actionError = action.ErrorMessage;

                if (action.OpenSelectedItem)
                {
                    actionError = TryOpenSelectedItem(state.SelectedResult);
                }
                else if (action.RevealSelectedItemInExplorer)
                {
                    actionError = TryRevealSelectedItemInExplorer(state.SelectedResult);
                }

                state = state with
                {
                    Options = options with { QueryText = buffer.ToString() },
                    ErrorMessage = actionError,
                    ShowHelp = action.ToggleHelpRequested ? !state.ShowHelp : state.ShowHelp,
                    Focus = action.FocusOverride ?? state.Focus,
                    SelectedIndex = action.SelectedIndexOverride ?? state.SelectedIndex
                };

                if (state.SelectedResult is null)
                {
                    state = state with { SelectedIndex = 0 };
                }
                else if (state.SelectedIndex >= state.VisibleResults.Count)
                {
                    state = state with { SelectedIndex = state.VisibleResults.Count - 1 };
                }

                var selectionChanged = previousSelection != state.SelectedIndex;
                if (selectionChanged)
                {
                    state = state with
                    {
                        Preview = _previewService.Build(state.SelectedResult),
                        PreviewScrollOffset = 0,
                        PreviewHorizontalOffset = 0
                    };
                }
                else if (action.ResetPreviewScroll)
                {
                    state = state with { PreviewScrollOffset = 0, PreviewHorizontalOffset = 0 };
                }
                else if (action.PreviewScrollDelta != 0)
                {
                    state = state with
                    {
                        PreviewScrollOffset = state.PreviewScrollOffset + action.PreviewScrollDelta
                    };
                }
                else if (action.PreviewScrollOverride.HasValue)
                {
                    state = state with { PreviewScrollOffset = action.PreviewScrollOverride.Value };
                }

                if (action.PreviewHorizontalDelta != 0)
                {
                    state = state with
                    {
                        PreviewHorizontalOffset = state.PreviewHorizontalOffset + action.PreviewHorizontalDelta
                    };
                }
                else if (action.PreviewHorizontalOverride.HasValue)
                {
                    state = state with { PreviewHorizontalOffset = action.PreviewHorizontalOverride.Value };
                }

                state = ClampViewportState(
                    state,
                    finderRenderer.VisiblePreviewLines,
                    finderRenderer.VisiblePreviewWidth);

                pendingSearch = action.TriggerSearch;
            }
        }
        finally
        {
            Console.TreatControlCAsInput = originalTreatControlCAsInput;
            Console.Clear();
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
                PreviewScrollOffset = 0,
                PreviewHorizontalOffset = 0,
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
                PreviewScrollOffset = 0,
                PreviewHorizontalOffset = 0,
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

    private static LiveFinderAction HandleKey(
        ConsoleKeyInfo key,
        FinderViewState state,
        StringBuilder query,
        int visiblePreviewLines)
    {
        var options = state.Options;

        if (!char.IsControl(key.KeyChar) && key.Key != ConsoleKey.Backspace)
        {
            query.Append(key.KeyChar);
            return LiveFinderAction.Search(options with { Offset = 0 }, selectedIndexOverride: 0);
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return HandleControlKey(key, options, query, visiblePreviewLines);
        }

        if (state.Focus == FinderPaneFocus.Preview && !state.ShowHelp)
        {
            return HandlePreviewKey(key, state);
        }

        return key.Key switch
        {
            ConsoleKey.Backspace => HandleBackspace(options, query),
            ConsoleKey.Enter => LiveFinderAction.Search(options with { Offset = 0 }, selectedIndexOverride: state.SelectedIndex, resetPreviewScroll: false),
            ConsoleKey.Escape => LiveFinderAction.Exit(options),
            ConsoleKey.UpArrow => HandleMoveUp(state),
            ConsoleKey.DownArrow => HandleMoveDown(state),
            ConsoleKey.PageDown => HandlePageDown(state),
            ConsoleKey.PageUp => HandlePageUp(state),
            ConsoleKey.Tab => LiveFinderAction.SetFocus(options, NextFocus(state.Focus)),
            ConsoleKey.F1 => LiveFinderAction.ToggleHelp(options, resetPreviewScroll: false),
            ConsoleKey.F2 => LiveFinderAction.UpdateOptions(options with { ShowDetails = !options.ShowDetails }, resetPreviewScroll: false),
            ConsoleKey.F3 => LiveFinderAction.Search(options with
            {
                Sort = SearchSortParser.Next(options.Sort),
                Offset = 0
            }, selectedIndexOverride: 0),
            ConsoleKey.F4 => LiveFinderAction.Search(options with { Regex = !options.Regex, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.F5 => LiveFinderAction.Search(options, selectedIndexOverride: state.SelectedIndex),
            ConsoleKey.F6 => LiveFinderAction.OpenItem(options),
            ConsoleKey.F7 => LiveFinderAction.RevealItem(options),
            _ => LiveFinderAction.None(options)
        };
    }

    private static LiveFinderAction HandlePreviewKey(ConsoleKeyInfo key, FinderViewState state)
    {
        var previewPage = Math.Max(1, (int)state.Options.Limit / 2);

        return key.Key switch
        {
            ConsoleKey.UpArrow => LiveFinderAction.ScrollPreview(state.Options, -1),
            ConsoleKey.DownArrow => LiveFinderAction.ScrollPreview(state.Options, 1),
            ConsoleKey.LeftArrow => LiveFinderAction.ScrollPreviewHorizontal(state.Options, -4),
            ConsoleKey.RightArrow => LiveFinderAction.ScrollPreviewHorizontal(state.Options, 4),
            ConsoleKey.PageUp => LiveFinderAction.ScrollPreview(state.Options, -previewPage),
            ConsoleKey.PageDown => LiveFinderAction.ScrollPreview(state.Options, previewPage),
            ConsoleKey.Home => LiveFinderAction.SetPreviewScrollHorizontal(state.Options, 0),
            ConsoleKey.End => LiveFinderAction.SetPreviewScrollHorizontal(state.Options, int.MaxValue),
            _ => key.Key switch
            {
                ConsoleKey.Tab => LiveFinderAction.SetFocus(state.Options, NextFocus(state.Focus)),
                ConsoleKey.Escape => LiveFinderAction.Exit(state.Options),
                _ => LiveFinderAction.None(state.Options)
            }
        };
    }

    private static LiveFinderAction HandleControlKey(
        ConsoleKeyInfo key,
        SearchCliOptions options,
        StringBuilder query,
        int visiblePreviewLines)
    {
        var previewPage = Math.Max(1, visiblePreviewLines / 2);

        return key.Key switch
        {
            ConsoleKey.L => ClearQuery(options, query),
            ConsoleKey.F => LiveFinderAction.Search(options with { ResultType = SearchResultType.Files, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.D => LiveFinderAction.ScrollPreview(options, previewPage),
            ConsoleKey.R => LiveFinderAction.Search(options with { Regex = !options.Regex, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.P => LiveFinderAction.Search(options with { MatchPath = !options.MatchPath, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.W => LiveFinderAction.Search(options with { MatchWholeWord = !options.MatchWholeWord, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.C => LiveFinderAction.Search(options with { MatchCase = !options.MatchCase, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.A => LiveFinderAction.Search(options with { ResultType = SearchResultType.All, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.O => LiveFinderAction.Search(options with { ResultType = SearchResultType.Folders, Offset = 0 }, selectedIndexOverride: 0),
            ConsoleKey.H => LiveFinderAction.ToggleHelp(options, resetPreviewScroll: false),
            ConsoleKey.Q => LiveFinderAction.Exit(options),
            ConsoleKey.U => LiveFinderAction.ScrollPreview(options, -previewPage),
            ConsoleKey.DownArrow => LiveFinderAction.ScrollPreview(options, 1),
            ConsoleKey.UpArrow => LiveFinderAction.ScrollPreview(options, -1),
            ConsoleKey.LeftArrow => LiveFinderAction.ScrollPreviewHorizontal(options, -12),
            ConsoleKey.RightArrow => LiveFinderAction.ScrollPreviewHorizontal(options, 12),
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
        return LiveFinderAction.Search(options with { Offset = 0 }, selectedIndexOverride: 0);
    }

    private static LiveFinderAction ClearQuery(SearchCliOptions options, StringBuilder query)
    {
        query.Clear();
        return LiveFinderAction.Search(options with { Offset = 0 }, selectedIndexOverride: 0);
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

    private static FinderPaneFocus NextFocus(FinderPaneFocus current) =>
        current == FinderPaneFocus.Results
            ? FinderPaneFocus.Preview
            : FinderPaneFocus.Results;

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

    private static LiveFinderAction HandleMoveUp(FinderViewState state)
    {
        if (state.SelectedIndex > 0)
        {
            return LiveFinderAction.Select(state.Options, state.SelectedIndex - 1);
        }

        if (state.Options.Offset == 0)
        {
            return LiveFinderAction.None(state.Options);
        }

        var previousOffset = state.Options.Offset > state.Options.Limit
            ? state.Options.Offset - state.Options.Limit
            : 0;

        return LiveFinderAction.Search(
            state.Options with { Offset = previousOffset },
            selectedIndexOverride: int.MaxValue);
    }

    private static LiveFinderAction HandleMoveDown(FinderViewState state)
    {
        if (state.SelectedIndex + 1 < state.VisibleResults.Count)
        {
            return LiveFinderAction.Select(state.Options, state.SelectedIndex + 1);
        }

        if (state.VisibleResults.Count == 0 || state.VisibleResults.Count < state.Options.Limit)
        {
            return LiveFinderAction.None(state.Options);
        }

        return LiveFinderAction.Search(
            state.Options with { Offset = state.Options.Offset + state.Options.Limit },
            selectedIndexOverride: 0);
    }

    private static LiveFinderAction HandlePageUp(FinderViewState state)
    {
        if (state.Options.Offset == 0)
        {
            return LiveFinderAction.None(state.Options);
        }

        var previousOffset = state.Options.Offset > state.Options.Limit
            ? state.Options.Offset - state.Options.Limit
            : 0;

        return LiveFinderAction.Search(
            state.Options with { Offset = previousOffset },
            selectedIndexOverride: int.MaxValue);
    }

    private static LiveFinderAction HandlePageDown(FinderViewState state)
    {
        if (state.VisibleResults.Count == 0 || state.VisibleResults.Count < state.Options.Limit)
        {
            return LiveFinderAction.None(state.Options);
        }

        return LiveFinderAction.Search(
            state.Options with { Offset = state.Options.Offset + state.Options.Limit },
            selectedIndexOverride: 0);
    }

    private static FinderViewState ClampViewportState(
        FinderViewState state,
        int visiblePreviewLines,
        int visiblePreviewWidth)
    {
        var safePreviewLines = Math.Max(1, visiblePreviewLines);
        var safePreviewWidth = Math.Max(1, visiblePreviewWidth);
        var visibleResults = state.VisibleResults;

        var selectedIndex = visibleResults.Count == 0
            ? 0
            : Math.Clamp(state.SelectedIndex, 0, visibleResults.Count - 1);

        var previewLineCount = FinderTuiRenderer.GetPreviewLineCount(state);
        var maxPreviewOffset = Math.Max(0, previewLineCount - safePreviewLines);
        var previewScrollOffset = state.PreviewScrollOffset == int.MaxValue
            ? maxPreviewOffset
            : Math.Clamp(state.PreviewScrollOffset, 0, maxPreviewOffset);
        var maxPreviewHorizontalOffset = Math.Max(0, GetMaxPreviewLineWidth(state) - safePreviewWidth);
        var previewHorizontalOffset = state.PreviewHorizontalOffset == int.MaxValue
            ? maxPreviewHorizontalOffset
            : Math.Clamp(state.PreviewHorizontalOffset, 0, maxPreviewHorizontalOffset);

        return state with
        {
            SelectedIndex = selectedIndex,
            PreviewScrollOffset = previewScrollOffset,
            PreviewHorizontalOffset = previewHorizontalOffset
        };
    }

    private static int GetMaxPreviewLineWidth(FinderViewState state)
    {
        var lines = state.ShowHelp
            ? FinderTuiRenderer.GetHelpLines()
            : state.Preview.Lines;

        var maxWidth = 0;
        foreach (var line in lines)
        {
            var width = line.Spans.Sum(static span => span.Text.Length);
            if (width > maxWidth)
            {
                maxWidth = width;
            }
        }

        return maxWidth + (state.Preview.IsTextPreview && !state.ShowHelp ? 5 : 0);
    }

    private static string? TryOpenSelectedItem(EverythingSearchResult? selected)
    {
        if (selected is null)
        {
            return "No item selected.";
        }

        if (!File.Exists(selected.FullPath) && !Directory.Exists(selected.FullPath))
        {
            return "Selected item is not available on disk.";
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = selected.FullPath,
                UseShellExecute = true
            });

            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to open selected item: {ex.Message}";
        }
    }

    private static string? TryRevealSelectedItemInExplorer(EverythingSearchResult? selected)
    {
        if (selected is null)
        {
            return "No item selected.";
        }

        if (!File.Exists(selected.FullPath) && !Directory.Exists(selected.FullPath))
        {
            return "Selected item is not available on disk.";
        }

        try
        {
            var explorerArgs = selected.IsFolder
                ? $"\"{selected.FullPath}\""
                : $"/select,\"{selected.FullPath}\"";

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = explorerArgs,
                UseShellExecute = true
            });

            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to reveal selected item: {ex.Message}";
        }
    }

    private sealed record LiveFinderAction(
        SearchCliOptions Options,
        bool TriggerSearch,
        bool ExitRequested,
        bool ToggleHelpRequested,
        bool ResetPreviewScroll,
        string? ErrorMessage,
        FinderPaneFocus? FocusOverride,
        int? SelectedIndexOverride,
        int PreviewScrollDelta,
        int? PreviewScrollOverride,
        int PreviewHorizontalDelta,
        int? PreviewHorizontalOverride,
        bool OpenSelectedItem,
        bool RevealSelectedItemInExplorer)
    {
        public static LiveFinderAction None(SearchCliOptions options) =>
            new(options, false, false, false, false, null, null, null, 0, null, 0, null, false, false);

        public static LiveFinderAction Search(
            SearchCliOptions options,
            int? selectedIndexOverride,
            bool resetPreviewScroll = true) =>
            new(options, true, false, false, resetPreviewScroll, null, null, selectedIndexOverride, 0, null, 0, null, false, false);

        public static LiveFinderAction Exit(SearchCliOptions options) =>
            new(options, false, true, false, false, null, null, null, 0, null, 0, null, false, false);

        public static LiveFinderAction ToggleHelp(SearchCliOptions options, bool resetPreviewScroll) =>
            new(options, false, false, true, resetPreviewScroll, null, null, null, 0, null, 0, null, false, false);

        public static LiveFinderAction Select(SearchCliOptions options, int selectedIndex) =>
            new(options, false, false, false, true, null, null, selectedIndex, 0, null, 0, null, false, false);

        public static LiveFinderAction UpdateOptions(SearchCliOptions options, bool resetPreviewScroll) =>
            new(options, false, false, false, resetPreviewScroll, null, null, null, 0, null, 0, null, false, false);

        public static LiveFinderAction SetFocus(SearchCliOptions options, FinderPaneFocus focus) =>
            new(options, false, false, false, false, null, focus, null, 0, null, 0, null, false, false);

        public static LiveFinderAction ScrollPreview(SearchCliOptions options, int previewScrollDelta) =>
            new(options, false, false, false, false, null, null, null, previewScrollDelta, null, 0, null, false, false);

        public static LiveFinderAction SetPreviewScroll(SearchCliOptions options, int previewScrollOffset) =>
            new(options, false, false, false, false, null, null, null, 0, previewScrollOffset, 0, null, false, false);

        public static LiveFinderAction ScrollPreviewHorizontal(SearchCliOptions options, int previewHorizontalDelta) =>
            new(options, false, false, false, false, null, null, null, 0, null, previewHorizontalDelta, null, false, false);

        public static LiveFinderAction SetPreviewScrollHorizontal(SearchCliOptions options, int previewHorizontalOffset) =>
            new(options, false, false, false, false, null, null, null, 0, null, 0, previewHorizontalOffset, false, false);

        public static LiveFinderAction OpenItem(SearchCliOptions options) =>
            new(options, false, false, false, false, null, null, null, 0, null, 0, null, true, false);

        public static LiveFinderAction RevealItem(SearchCliOptions options) =>
            new(options, false, false, false, false, null, null, null, 0, null, 0, null, false, true);
    }
}
