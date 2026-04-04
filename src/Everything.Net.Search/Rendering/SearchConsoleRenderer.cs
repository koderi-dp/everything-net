using Everything.Net.Models;
using Everything.Net.Search.Models;
using Everything.Net.Search.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Everything.Net.Search.Rendering;

internal static class SearchConsoleRenderer
{
    public static void RenderHeader()
    {
        AnsiConsole.Write(
            new Rows(
            [
                new FigletText("Everything")
                    .LeftJustified()
                    .Color(Color.CadetBlue),
                new Markup("[grey]Interactive file and folder search powered by Everything.Net[/]"),
                new Rule().RuleStyle("grey")
            ]));
    }

    public static IRenderable RenderLiveFinder(FinderViewState state)
    {
        var root = new Layout("Root");
        root.SplitRows(
            new Layout("Top").Size(3),
            new Layout("Body"),
            new Layout("Footer").Size(3));

        root["Top"].Update(BuildFinderHeader(state));
        root["Footer"].Update(BuildFooter(state));

        root["Body"].SplitColumns(
            new Layout("Results").Ratio(7),
            new Layout("Preview").Ratio(5));

        root["Body"]["Results"].Update(BuildResultsPane(state));
        root["Body"]["Preview"].Update(BuildPreviewPane(state));

        return root;
    }

    public static void RenderResults(EverythingQueryResponse response, SearchCliOptions options)
    {
        var filteredResults = Filter(response.Results, options.ResultType).ToList();

        var summary = new Grid();
        summary.AddColumn();
        summary.AddColumn();
        summary.AddRow("[grey]Query[/]", Markup.Escape(response.SearchText));
        summary.AddRow("[grey]Visible[/]", filteredResults.Count.ToString());
        summary.AddRow("[grey]Matched[/]", response.TotalResults.ToString());
        summary.AddRow("[grey]Type[/]", FormatResultType(options.ResultType));
        summary.AddRow("[grey]Sort[/]", SearchSortParser.Display(options.Sort));

        var usage = new Rows(
        [
            new Markup("[grey]Usage[/]"),
            new Markup("Launch without a query to open the live finder."),
            new Markup("Use Everything operators like [blue]dm:today[/], [blue]ext:cs[/], [blue]parent:C:\\Source[/].")
        ]);

        AnsiConsole.Write(new Columns(
        [
            new Panel(summary).Header("Search Summary").Border(BoxBorder.Rounded),
            new Panel(usage).Header("Tips").Border(BoxBorder.Rounded)
        ]));
        AnsiConsole.Write(new Rule("[grey]Results[/]").RuleStyle("grey"));

        if (filteredResults.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results matched the current query/filter.[/]");
            return;
        }

        AnsiConsole.Write(BuildResultsTable(filteredResults, options, selectedIndex: null));
    }

    public static void RenderHelp()
    {
        var table = new Table().Border(TableBorder.MinimalHeavyHead);
        table.AddColumn("Option");
        table.AddColumn("Description");
        table.AddRow("`Everything.Net.Search <query>`", "Run a one-shot search from the command line.");
        table.AddRow("`-n`, `--limit <number>`", "Maximum number of results to request. Default: 25.");
        table.AddRow("`--offset <number>`", "Skip this many results before returning items.");
        table.AddRow("`--sort <name>`", "Sort by `modified`, `name`, `path`, `size`, `extension`, `created`, `accessed`.");
        table.AddRow("`--files`", "Show files only.");
        table.AddRow("`--folders`", "Show folders only.");
        table.AddRow("`--details`", "Include size and modified time columns.");
        table.AddRow("`--no-details`", "Hide extra columns for a tighter table.");
        table.AddRow("`--regex`", "Treat the query as a regex.");
        table.AddRow("`--match-case`", "Enable case-sensitive matching.");
        table.AddRow("`--whole-word`", "Require whole-word matching.");
        table.AddRow("`--match-path`", "Include path text in matching.");
        table.AddRow("`--no-prompt`", "Disable interactive mode when no query is passed.");
        table.AddRow("`-h`, `--help`", "Show help.");

        AnsiConsole.Write(new Panel(table).Header("Everything.Net.Search"));
    }

    public static void RenderUnavailable()
    {
        AnsiConsole.MarkupLine("[red]Everything is not available.[/]");
        AnsiConsole.MarkupLine("Make sure Everything is installed, running, and the native SDK DLLs are present.");
    }

    public static void RenderSearchFailure(EverythingQueryResponse response)
    {
        AnsiConsole.MarkupLine($"[red]Search failed:[/] {Markup.Escape(response.ErrorMessage ?? response.ErrorCode.ToString())}");
    }

    private static Panel BuildFinderHeader(FinderViewState state)
    {
        var query = string.IsNullOrWhiteSpace(state.Options.QueryText)
            ? "[grey dim](type to search)[/]"
            : Markup.Escape(state.Options.QueryText);

        var line = new Grid();
        line.AddColumn(new GridColumn().NoWrap());
        line.AddColumn();
        line.AddColumn(new GridColumn().RightAligned().NoWrap());
        line.AddRow(
            $"[bold deepskyblue1]>[/] {query}",
            $"{FormatResultType(state.Options.ResultType)} | {SearchSortParser.Display(state.Options.Sort)} | {BuildFlagsSummary(state.Options)} | offset {state.Options.Offset}",
            $"[grey]{state.VisibleResults.Count} / {state.Response?.TotalResults ?? 0}[/]");

        return new Panel(line)
            .Header("Finder")
            .Border(BoxBorder.Rounded);
    }

    private static Panel BuildResultsPane(FinderViewState state)
    {
        if (!string.IsNullOrWhiteSpace(state.ErrorMessage))
        {
            return new Panel(new Markup($"[red]{Markup.Escape(state.ErrorMessage)}[/]"))
                .Header("Results")
                .Border(BoxBorder.Rounded)
                .Expand();
        }

        if (string.IsNullOrWhiteSpace(state.Options.QueryText))
        {
            return new Panel(new Rows(
            [
                new Markup("[grey]Start typing to search.[/]"),
                new Markup("Use [blue]Tab[/] to cycle file/folder filters."),
                new Markup("Use [blue]F1[/] to show the live help panel.")
            ]))
            .Header("Results")
            .Border(BoxBorder.Rounded)
            .Expand();
        }

        if (state.Response is null || state.VisibleResults.Count == 0)
        {
            return new Panel(new Markup("[yellow]No results matched the current query/filter.[/]"))
                .Header("Results")
                .Border(BoxBorder.Rounded)
                .Expand();
        }

        return new Panel(BuildResultsTable(state.VisibleResults, state.Options, state.SelectedIndex))
            .Header($"Results ({state.VisibleResults.Count})")
            .Border(BoxBorder.Rounded)
            .Expand();
    }

    private static Panel BuildPreviewPane(FinderViewState state)
    {
        if (state.ShowHelp)
        {
            return BuildHelpPane().Expand();
        }

        var selected = state.SelectedResult;
        if (selected is null)
        {
            return new Panel(new Rows(
            [
                new Markup("[grey]No item selected.[/]"),
                new Markup("Use [blue]Up[/] and [blue]Down[/] to move once results appear.")
            ]))
            .Header("Preview")
            .Border(BoxBorder.Rounded)
            .Expand();
        }

        var previewBody = state.Preview.IsTextPreview
            ? BuildTextPreview(state.Preview)
            : BuildMetadataPreview(state.Preview, selected);

        return new Panel(new Rows(
        [
            BuildPreviewPath(selected),
            new Rule($"[grey]{Markup.Escape(state.Preview.IsTextPreview ? "Content" : "Metadata")}[/]").RuleStyle("grey"),
            previewBody
        ]))
        .Header("Preview")
        .Border(BoxBorder.Rounded)
        .Expand();
    }

    private static Panel BuildHelpPane()
    {
        var rows = new Rows(
        [
            new Markup("[grey]Keybindings[/]"),
            new Markup("[blue]Type[/] search live"),
            new Markup("[blue]Backspace[/] delete character"),
            new Markup("[blue]Up/Down[/] move selection"),
            new Markup("[blue]PageUp/PageDown[/] page results"),
            new Markup("[blue]Tab[/] cycle all/files/folders"),
            new Markup("[blue]F1[/] toggle help"),
            new Markup("[blue]F2[/] toggle details"),
            new Markup("[blue]F3[/] cycle sort"),
            new Markup("[blue]F4[/] toggle regex"),
            new Markup("[blue]F5[/] refresh"),
            new Markup("[blue]Ctrl+L[/] clear query"),
            new Markup("[blue]Ctrl+C[/] toggle case"),
            new Markup("[blue]Ctrl+W[/] toggle whole-word"),
            new Markup("[blue]Ctrl+P[/] toggle path matching"),
            new Markup("[blue]Esc[/] exit")
        ]);

        return new Panel(rows)
            .Header("Help")
            .Border(BoxBorder.Rounded);
    }

    private static Panel BuildFooter(FinderViewState state)
    {
        var status = state.SelectedResult is null
            ? "No selection"
            : $"Selected: {Markup.Escape(state.SelectedResult.FileName)}";

        var footer = new Grid();
        footer.AddColumn();
        footer.AddColumn(new GridColumn().RightAligned());
        footer.AddRow(
            "[grey]keys[/] [blue]Tab[/] filter  [blue]F1[/] help  [blue]F2[/] details  [blue]F3[/] sort  [blue]F4[/] regex  [blue]Esc[/] exit",
            status);

        return new Panel(footer).Border(BoxBorder.Rounded);
    }

    private static Table BuildResultsTable(
        IReadOnlyList<EverythingSearchResult> results,
        SearchCliOptions options,
        int? selectedIndex)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .Expand()
            .AddColumn("#")
            .AddColumn("T")
            .AddColumn("Name")
            .AddColumn("Parent");

        if (options.ShowDetails)
        {
            table.AddColumn(new TableColumn("Size").RightAligned());
            table.AddColumn("Modified");
        }

        for (var index = 0; index < results.Count; index++)
        {
            var item = results[index];
            var isSelected = selectedIndex.HasValue && selectedIndex.Value == index;
            var prefix = isSelected ? "[black on lightskyblue1]>[/]" : " ";
            var typeMarkup = item.IsFolder ? "[blue]d[/]" : "[green]f[/]";
            var rowNumber = (options.Offset + (uint)index + 1).ToString();
            var nameText = Truncate(item.FileName, item.IsFolder ? 24 : 26);
            var name = item.IsFolder ? $"[bold]{Escape(nameText)}[/]" : Escape(nameText);
            var directory = Escape(Truncate(CompactParentPath(item.Path), 28));

            if (options.ShowDetails)
            {
                table.AddRow(
                    $"{prefix} {rowNumber}",
                    typeMarkup,
                    name,
                    directory,
                    item.IsFolder ? "-" : FormatSize(item.Size),
                    FormatDate(item.DateModified));
            }
            else
            {
                table.AddRow($"{prefix} {rowNumber}", typeMarkup, name, directory);
            }
        }

        return table;
    }

    private static IRenderable BuildTextPreview(FinderPreviewContent preview)
    {
        var rows = preview.Lines
            .Select((line, index) => new Markup($"[grey]{index + 1,2}[/] {Escape(line)}"))
            .Cast<IRenderable>()
            .ToArray();

        return rows.Length == 0 ? new Markup("[grey]No preview content.[/]") : new Rows(rows);
    }

    private static IRenderable BuildMetadataPreview(FinderPreviewContent preview, EverythingSearchResult selected)
    {
        var info = new Grid();
        info.AddColumn(new GridColumn().NoWrap());
        info.AddColumn();
        info.AddRow("[grey]Type[/]", selected.IsFolder ? "Folder" : "File");
        info.AddRow("[grey]Name[/]", Escape(Truncate(selected.FileName, 34)));
        info.AddRow("[grey]Path[/]", Escape(Truncate(selected.Path, 48)));
        info.AddRow("[grey]Full path[/]", Escape(Truncate(selected.FullPath, 48)));
        info.AddRow("[grey]Extension[/]", Escape(selected.Extension ?? "-"));
        info.AddRow("[grey]Size[/]", selected.IsFolder ? "-" : FormatSize(selected.Size));
        info.AddRow("[grey]Modified[/]", FormatDate(selected.DateModified));

        return info;
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

    private static IRenderable BuildPreviewPath(EverythingSearchResult result)
    {
        var path = string.IsNullOrWhiteSpace(result.FullPath) ? result.FileName : result.FullPath;
        return new TextPath(Truncate(path, 72))
            .RootColor(Color.Grey)
            .SeparatorColor(Color.Grey)
            .StemColor(result.IsFolder ? Color.CornflowerBlue : Color.Green);
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

    private static string Escape(string value) => Markup.Escape(string.IsNullOrWhiteSpace(value) ? "-" : value);

    private static string CompactParentPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ".";
        }

        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2)
        {
            return normalized;
        }

        return $"{parts[0]}/.../{parts[^1]}";
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        if (maxLength <= 3)
        {
            return value[..maxLength];
        }

        return $"{value[..(maxLength - 3)]}...";
    }

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
