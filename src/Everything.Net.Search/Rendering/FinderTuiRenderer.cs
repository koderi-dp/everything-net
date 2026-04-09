using Everything.Net.Models;
using Everything.Net.Search.Models;
using Everything.Net.Search.Services;
using Spectre.Console;
using Spectre.Tui;

namespace Everything.Net.Search.Rendering;

internal sealed class FinderTuiRenderer
{
    private static readonly Color BaseBackgroundColor = Color.Default;
    private static readonly Color BorderColor = new(126, 118, 148);
    private static readonly Color AccentColor = new(214, 176, 112);
    private static readonly Color SearchColor = new(214, 118, 152);
    private static readonly Color PrimaryTextColor = new(232, 232, 238);
    private static readonly Color DimTextColor = new(150, 150, 164);
    private static readonly Color SelectedBackgroundColor = new(63, 60, 79);
    private static readonly Color SelectedTextColor = new(246, 246, 249);

    private static readonly Style BaseStyle = new(PrimaryTextColor, BaseBackgroundColor);
    private static readonly Style PanelStyle = new(PrimaryTextColor, BaseBackgroundColor);
    private static readonly Style DimStyle = new(DimTextColor, BaseBackgroundColor);
    private static readonly Style BorderStyle = new(BorderColor, BaseBackgroundColor);
    private static readonly Style SelectedStyle = new(SelectedTextColor, SelectedBackgroundColor);
    private static readonly Style HeaderStyle = new(AccentColor, BaseBackgroundColor);
    private static readonly Style SearchStyle = new(SearchColor, BaseBackgroundColor);
    private static readonly Style PreviewTextStyle = new(PrimaryTextColor, BaseBackgroundColor);
    private static readonly Style PreviewLineNumberStyle = new(DimTextColor, BaseBackgroundColor);
    private static readonly Style ActiveBorderStyle = new(AccentColor, BaseBackgroundColor);
    private static readonly string[] HelpLines =
    [
        "Type to search live",
        "Backspace delete character",
        "Up/Down move selection",
        "PageUp/PageDown page results",
        "Ctrl+Up/Ctrl+Down scroll preview",
        "Ctrl+U/Ctrl+D half-page preview scroll",
        "Tab cycle all/files/folders",
        "F1 toggle help",
        "F2 toggle details",
        "F3 cycle sort",
        "F4 toggle regex",
        "F5 refresh",
        "F6 open selected item",
        "F7 reveal selected item in Explorer",
        "Ctrl+L clear query",
        "Ctrl+C toggle case",
        "Ctrl+W toggle whole-word",
        "Ctrl+P toggle path matching",
        "Esc exit"
    ];
    private static readonly FinderPreviewLine[] HelpPreviewLines = HelpLines.Select(FinderPreviewLine.Plain).ToArray();

    private readonly ListWidget<FinderResultListItem> _results = new(new List<FinderResultListItem>())
    {
        HighlightSymbol = "> ",
        HighlightStyle = SelectedStyle
    };

    public int VisibleResultRows { get; private set; } = 10;
    public int VisiblePreviewLines { get; private set; } = 10;
    public int VisiblePreviewWidth { get; private set; } = 10;

    public void Render(RenderContext context, FinderViewState state)
    {
        context.Render(new ClearWidget(' ', BaseStyle));

        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Top").Size(4),
                new Layout("Body"),
                new Layout("Bottom").Size(2));

        layout.GetLayout("Body").SplitColumns(
            new Layout("Results").Ratio(7),
            new Layout("Preview").Ratio(5));

        RenderTop(context, layout.GetArea(context, "Top"), state);
        RenderResultsPane(context, layout.GetArea(context, "Results"), state);
        RenderPreviewPane(context, layout.GetArea(context, "Preview"), state);
        RenderBottom(context, layout.GetArea(context, "Bottom"), state);
    }

    public static int GetPreviewLineCount(FinderViewState state)
    {
        return state.ShowHelp ? HelpLines.Length : state.Preview.Lines.Count;
    }

    public static IReadOnlyList<FinderPreviewLine> GetHelpLines() => HelpPreviewLines;

    private void RenderTop(RenderContext context, Rectangle area, FinderViewState state)
    {
        var query = string.IsNullOrWhiteSpace(state.Options.QueryText)
            ? "(type to search)"
            : state.Options.QueryText;
        var lines = new Text(
        [
            new TextLine(
            [
                new TextSpan("> ", SearchStyle),
                new TextSpan(EscapeMarkup(query), new Style(PrimaryTextColor, BaseBackgroundColor))
            ]),
            TextLine.FromString(string.Empty),
            TextLine.FromMarkup(
                $"[grey]{FormatResultType(state.Options.ResultType)}[/] | " +
                $"[grey]{EscapeMarkup(SearchSortParser.Display(state.Options.Sort))}[/] | " +
                $"[grey]{EscapeMarkup(BuildFlagsSummary(state.Options))}[/] | " +
                $"[grey]offset {state.Options.Offset}[/]"),
            TextLine.FromMarkup(
                $"[grey]{state.VisibleResults.Count} visible / {state.Response?.TotalResults ?? 0} matched[/]")
        ]);

        context.Render(lines, area);
    }

    private void RenderResultsPane(RenderContext context, Rectangle area, FinderViewState state)
    {
        context.Render(new BoxWidget(state.Focus == FinderPaneFocus.Results ? ActiveBorderStyle : BorderStyle) { Border = Border.Rounded }, area);

        var inner = area.Inflate(-1, -1);
        if (inner.IsEmpty)
        {
            VisibleResultRows = 1;
            return;
        }

        context.Render(new ClearWidget(' ', PanelStyle), inner);

        var titleArea = new Rectangle(inner.X, inner.Y, inner.Width, Math.Min(1, inner.Height));
        var contentArea = new Rectangle(
            inner.X,
            inner.Y + 1,
            inner.Width,
            Math.Max(0, inner.Height - 1));

        VisibleResultRows = Math.Max(1, contentArea.Height / FinderResultListItem.GetItemHeight(state.Options));

        context.Render(new Text(
        [
            new TextLine(
            [
                new TextSpan("Results", HeaderStyle),
                new TextSpan($" ({state.VisibleResults.Count})", DimStyle)
            ])
        ]), titleArea);

        if (contentArea.IsEmpty)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
        {
            context.Render(Text.FromMarkup($"[red]{EscapeMarkup(state.ErrorMessage)}[/]"), contentArea);
            return;
        }

        if (string.IsNullOrWhiteSpace(state.Options.QueryText))
        {
            context.Render(
                new Text(
                [
                    TextLine.FromString("Start typing to search.", DimStyle),
                    TextLine.FromString("Tab cycles file/folder filters.", DimStyle),
                    TextLine.FromString("F1 opens the help pane.", DimStyle)
                ]),
                contentArea);
            return;
        }

        if (state.Response is null || state.VisibleResults.Count == 0)
        {
            context.Render(Text.FromMarkup("[yellow]No results matched the current query/filter.[/]"), contentArea);
            return;
        }

        _results.Items.Clear();
        for (var index = 0; index < state.VisibleResults.Count; index++)
        {
            _results.Items.Add(new FinderResultListItem(state.VisibleResults[index], state.Options, index));
        }

        _results.SelectedIndex = state.VisibleResults.Count == 0 ? null : state.SelectedIndex;
        context.Render(_results, contentArea);
    }

    private void RenderPreviewPane(RenderContext context, Rectangle area, FinderViewState state)
    {
        context.Render(new BoxWidget(state.Focus == FinderPaneFocus.Preview ? ActiveBorderStyle : BorderStyle) { Border = Border.Rounded }, area);

        var inner = area.Inflate(-1, -1);
        if (inner.IsEmpty)
        {
            VisiblePreviewLines = 1;
            VisiblePreviewWidth = 1;
            return;
        }

        context.Render(new ClearWidget(' ', PanelStyle), inner);

        var detailHeight = state.ShowHelp || state.SelectedResult is null ? 0 : Math.Min(2, Math.Max(0, inner.Height - 4));
        var headerHeight = Math.Min(2, inner.Height);
        var headerArea = new Rectangle(inner.X, inner.Y, inner.Width, headerHeight);
        var contentArea = new Rectangle(
            inner.X,
            inner.Y + headerArea.Height,
            inner.Width,
            Math.Max(0, inner.Height - headerArea.Height - detailHeight));
        var detailArea = new Rectangle(
            inner.X,
            contentArea.Y + contentArea.Height,
            inner.Width,
            detailHeight);

        VisiblePreviewLines = Math.Max(1, contentArea.Height);
        VisiblePreviewWidth = Math.Max(1, contentArea.Width);

        var previewTitle = state.ShowHelp ? "Help" : "Preview";
        context.Render(
            new Text(
            [
                new TextLine(
                [
                    new TextSpan(previewTitle, HeaderStyle),
                    new TextSpan($" ({state.PreviewScrollOffset + 1}-{Math.Min(GetPreviewLineCount(state), state.PreviewScrollOffset + VisiblePreviewLines)} / {GetPreviewLineCount(state)})", DimStyle)
                ]),
                TextLine.FromString(state.ShowHelp ? "Keybindings" : string.Empty, DimStyle)
            ]),
            headerArea);

        if (contentArea.IsEmpty)
        {
            return;
        }

        var lines = state.ShowHelp ? HelpPreviewLines : state.Preview.Lines;
        context.Render(
            new FinderPreviewWidget(lines, state.PreviewScrollOffset, state.PreviewHorizontalOffset, state.Preview.IsTextPreview && !state.ShowHelp),
            contentArea);

        if (!detailArea.IsEmpty && state.SelectedResult is not null)
        {
            context.Render(BuildPreviewDetails(state.SelectedResult), detailArea);
        }
    }

    private void RenderBottom(RenderContext context, Rectangle area, FinderViewState state)
    {
        var status = state.SelectedResult is null
            ? "No selection"
            : $"Selected: {state.SelectedResult.FileName}";

        var lines = new Text(
        [
            TextLine.FromMarkup($"[grey]Tab[/] focus  [grey]F1[/] help  [grey]F2[/] details  [grey]F3[/] sort  [grey]F4[/] regex  [grey]PgUp/PgDn[/] {(state.Focus == FinderPaneFocus.Preview ? "preview" : "page")}"),
            TextLine.FromMarkup($"[grey]F6[/] open  [grey]F7[/] explorer  [grey]Focus:[/] {(state.Focus == FinderPaneFocus.Results ? "[yellow]results[/]" : "[yellow]preview[/]")}  [grey]Ctrl+F/A/O[/] filter  [grey]Esc[/] exit  [grey]{EscapeMarkup(status)}[/]")
        ]);

        context.Render(new ClearWidget(' ', BaseStyle), area);
        context.Render(lines, area);
    }

    private static string FormatResultType(SearchResultType type) => type switch
    {
        SearchResultType.Files => "files",
        SearchResultType.Folders => "folders",
        _ => "all"
    };

    private static string BuildFlagsSummary(SearchCliOptions options)
    {
        var flags = new List<string>();

        if (options.ShowDetails)
        {
            flags.Add("details");
        }

        if (options.Regex)
        {
            flags.Add("regex");
        }

        if (options.MatchCase)
        {
            flags.Add("case");
        }

        if (options.MatchWholeWord)
        {
            flags.Add("word");
        }

        if (options.MatchPath)
        {
            flags.Add("path");
        }

        return flags.Count == 0 ? "default" : string.Join(", ", flags);
    }

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

    private static string FormatDate(DateTimeOffset? value) => value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        if (maxLength <= 0 || value.Length <= maxLength)
        {
            return value;
        }

        if (maxLength <= 3)
        {
            return value[..maxLength];
        }

        return $"{value[..(maxLength - 3)]}...";
    }

    private static string EscapeMarkup(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("[", "[[").Replace("]", "]]");
    }

    private static Text BuildPreviewDetails(EverythingSearchResult result)
    {
        var typeLabel = result.IsFolder ? "Folder" : "File";
        var sizeLabel = result.IsFolder ? "-" : FormatSize(result.Size);
        var modifiedLabel = FormatDate(result.DateModified);
        var extensionLabel = result.Extension ?? "-";

        return new Text(
        [
            TextLine.FromString(new string('─', 64), new Style(BorderColor, BaseBackgroundColor)),
            new TextLine(
            [
                new TextSpan(typeLabel, DimStyle),
                new TextSpan("  ", DimStyle),
                new TextSpan(sizeLabel, DimStyle),
                new TextSpan("  ", DimStyle),
                new TextSpan(extensionLabel, DimStyle),
                new TextSpan("  ", DimStyle),
                new TextSpan($"modified {modifiedLabel}", DimStyle)
            ])
        ]);
    }

    private sealed class FinderResultListItem(
        EverythingSearchResult result,
        SearchCliOptions options,
        int index) : ListWidgetItem
    {
        public static int GetItemHeight(SearchCliOptions options) => options.ShowDetails ? 3 : 2;

        protected override Text CreateText(bool isSelected)
        {
            var rowNumber = $"{options.Offset + (uint)index + 1,4} ";
            var titleWidth = Math.Max(12, options.ShowDetails ? 58 : 70);
            var title = new TextLine(
            [
                new TextSpan(rowNumber, DimStyle),
                new TextSpan(
                    Truncate(result.FileName, titleWidth),
                    new Style(
                        isSelected ? SelectedTextColor : PrimaryTextColor,
                        isSelected ? SelectedBackgroundColor : BaseBackgroundColor))
            ]);

            var pathText = string.IsNullOrWhiteSpace(result.Path) ? "." : result.Path;

            var path = new TextLine(
            [
                new TextSpan(new string(' ', rowNumber.Length), DimStyle),
                new TextSpan(pathText, DimStyle)
            ]);

            if (options.ShowDetails)
            {
                var details = new TextLine(
                [
                    new TextSpan(new string(' ', rowNumber.Length), DimStyle),
                    new TextSpan($"modified {FormatDate(result.DateModified)}", DimStyle),
                    new TextSpan("  ", DimStyle),
                    new TextSpan(result.IsFolder ? "folder" : $"size {FormatSize(result.Size)}", DimStyle)
                ]);

                return new Text([title, path, details]);
            }

            return new Text([title, path]);
        }
    }

    private sealed class FinderPreviewWidget(
        IReadOnlyList<FinderPreviewLine> lines,
        int offset,
        int horizontalOffset,
        bool showLineNumbers) : IWidget
    {
        public void Render(RenderContext context)
        {
            var visibleLines = lines
                .Skip(offset)
                .Take(context.Viewport.Height)
                .ToArray();

            for (var index = 0; index < visibleLines.Length; index++)
            {
                var line = showLineNumbers
                    ? $"{offset + index + 1,4} "
                    : string.Empty;

                if (showLineNumbers)
                {
                    context.SetString(0, index, line, PreviewLineNumberStyle, maxWidth: Math.Min(line.Length, context.Viewport.Width));
                    RenderLine(context, line.Length, index, visibleLines[index], horizontalOffset);
                }
                else
                {
                    RenderLine(context, 0, index, visibleLines[index], horizontalOffset);
                }
            }
        }

        private static void RenderLine(RenderContext context, int startX, int y, FinderPreviewLine line, int horizontalOffset)
        {
            var x = Math.Min(startX, context.Viewport.Width);
            var remainingOffset = Math.Max(0, horizontalOffset);

            foreach (var span in line.Spans)
            {
                if (x >= context.Viewport.Width || string.IsNullOrEmpty(span.Text))
                {
                    break;
                }

                var text = span.Text;
                if (remainingOffset >= text.Length)
                {
                    remainingOffset -= text.Length;
                    continue;
                }

                if (remainingOffset > 0)
                {
                    text = text[remainingOffset..];
                    remainingOffset = 0;
                }

                var maxWidth = context.Viewport.Width - x;
                context.SetString(x, y, text, span.Style ?? PreviewTextStyle, maxWidth: maxWidth);
                x += Math.Min(text.Length, maxWidth);
            }
        }
    }
}
