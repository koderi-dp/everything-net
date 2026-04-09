namespace Everything.Net.Models;

/// <summary>
/// Represents a single search result returned by Everything.
/// </summary>
public sealed class EverythingSearchResult
{
    /// <summary>
    /// Gets the zero-based index of the result within the current response window.
    /// </summary>
    public required uint Index { get; init; }

    /// <summary>
    /// Gets the file or folder name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the parent directory path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the full path for the result.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Gets the file extension when requested and available.
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// Gets the file size in bytes when requested and available.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Gets the creation time when requested and available.
    /// </summary>
    public DateTimeOffset? DateCreated { get; init; }

    /// <summary>
    /// Gets the last modified time when requested and available.
    /// </summary>
    public DateTimeOffset? DateModified { get; init; }

    /// <summary>
    /// Gets the last accessed time when requested and available.
    /// </summary>
    public DateTimeOffset? DateAccessed { get; init; }

    /// <summary>
    /// Gets the native file attributes when requested and available.
    /// </summary>
    public uint? Attributes { get; init; }

    /// <summary>
    /// Gets the file list file name when requested and available.
    /// </summary>
    public string? FileListFileName { get; init; }

    /// <summary>
    /// Gets the run count when requested and available.
    /// </summary>
    public uint? RunCount { get; init; }

    /// <summary>
    /// Gets the last run time when requested and available.
    /// </summary>
    public DateTimeOffset? DateRun { get; init; }

    /// <summary>
    /// Gets the recently changed time when requested and available.
    /// </summary>
    public DateTimeOffset? DateRecentlyChanged { get; init; }

    /// <summary>
    /// Gets the highlighted file name fragment when requested and available.
    /// </summary>
    public string? HighlightedFileName { get; init; }

    /// <summary>
    /// Gets the highlighted path fragment when requested and available.
    /// </summary>
    public string? HighlightedPath { get; init; }

    /// <summary>
    /// Gets the highlighted full path when requested and available.
    /// </summary>
    public string? HighlightedFullPath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the result is a folder.
    /// </summary>
    public bool IsFolder { get; init; }

    /// <summary>
    /// Gets a value indicating whether the result is a file.
    /// </summary>
    public bool IsFile => !IsFolder;

    /// <summary>
    /// Returns the full path for debugging and display purposes.
    /// </summary>
    /// <returns>The full path of the result.</returns>
    public override string ToString() => FullPath;
}
