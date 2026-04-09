namespace Everything.Net.Search.Models;

internal sealed record FinderPreviewLine(
    IReadOnlyList<FinderPreviewSpan> Spans)
{
    public static FinderPreviewLine Plain(string text) =>
        new([new FinderPreviewSpan(text)]);
}
