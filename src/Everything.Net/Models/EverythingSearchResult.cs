namespace Everything.Net.Models;

public sealed class EverythingSearchResult
{
    public required uint Index { get; init; }

    public required string FileName { get; init; }

    public required string Path { get; init; }

    public required string FullPath { get; init; }

    public string? Extension { get; init; }

    public long? Size { get; init; }

    public DateTimeOffset? DateCreated { get; init; }

    public DateTimeOffset? DateModified { get; init; }

    public DateTimeOffset? DateAccessed { get; init; }

    public uint? Attributes { get; init; }

    public string? FileListFileName { get; init; }

    public uint? RunCount { get; init; }

    public DateTimeOffset? DateRun { get; init; }

    public DateTimeOffset? DateRecentlyChanged { get; init; }

    public string? HighlightedFileName { get; init; }

    public string? HighlightedPath { get; init; }

    public string? HighlightedFullPath { get; init; }

    public bool IsFolder { get; init; }

    public bool IsFile => !IsFolder;

    public override string ToString() => FullPath;
}
