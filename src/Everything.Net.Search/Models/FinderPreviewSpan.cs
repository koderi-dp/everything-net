using Spectre.Console;

namespace Everything.Net.Search.Models;

internal sealed record FinderPreviewSpan(
    string Text,
    Style? Style = null);
