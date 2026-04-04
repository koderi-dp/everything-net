namespace Everything.Net.Search.Models;

internal sealed record FinderPreviewContent(
    string Header,
    bool IsTextPreview,
    IReadOnlyList<string> Lines)
{
    public static FinderPreviewContent Empty(string header, params string[] lines) =>
        new(header, false, lines);
}
