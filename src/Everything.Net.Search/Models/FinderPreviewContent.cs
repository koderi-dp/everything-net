namespace Everything.Net.Search.Models;

internal sealed record FinderPreviewContent(
    string Header,
    bool IsTextPreview,
    IReadOnlyList<FinderPreviewLine> Lines)
{
    public static FinderPreviewContent Empty(string header, params string[] lines) =>
        new(header, false, lines.Select(FinderPreviewLine.Plain).ToArray());
}
