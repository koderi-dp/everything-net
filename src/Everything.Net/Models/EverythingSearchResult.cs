using System.Diagnostics.CodeAnalysis;
using Everything.Net.Internal;

namespace Everything.Net.Models;

/// <summary>
/// Represents a single search result returned by Everything.
/// </summary>
public sealed class EverythingSearchResult
{
    // Dates are kept as raw file times (0 = not set): 8 bytes each instead of 24.
    private long _dateCreated;
    private long _dateModified;
    private long _dateAccessed;
    private long _dateRun;
    private long _dateRecentlyChanged;
    private string? _fullPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="EverythingSearchResult"/> class.
    /// </summary>
    public EverythingSearchResult()
    {
    }

    /// <summary>
    /// Used by the client. A <see langword="null"/> full path is built from the path and file name on first use.
    /// </summary>
#pragma warning disable CS8618 // FullPath is never null to callers: the getter builds it when unset.
    [SetsRequiredMembers]
    internal EverythingSearchResult(uint index, string fileName, string path, string? fullPath)
    {
        Index = index;
        FileName = fileName;
        Path = path;
        _fullPath = fullPath;
    }
#pragma warning restore CS8618

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
    public required string FullPath
    {
        // Same rule the Everything SDK uses: no separator when there is no parent path.
        get => _fullPath ??= Path.Length == 0 ? FileName : string.Concat(Path, "\\", FileName);
        init => _fullPath = value;
    }

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
    public DateTimeOffset? DateCreated
    {
        get => FileTimeConverter.FromNative(_dateCreated);
        init => _dateCreated = FileTimeConverter.ToNative(value);
    }

    internal long DateCreatedFileTime
    {
        init => _dateCreated = value;
    }

    /// <summary>
    /// Gets the last modified time when requested and available.
    /// </summary>
    public DateTimeOffset? DateModified
    {
        get => FileTimeConverter.FromNative(_dateModified);
        init => _dateModified = FileTimeConverter.ToNative(value);
    }

    internal long DateModifiedFileTime
    {
        init => _dateModified = value;
    }

    /// <summary>
    /// Gets the last accessed time when requested and available.
    /// </summary>
    public DateTimeOffset? DateAccessed
    {
        get => FileTimeConverter.FromNative(_dateAccessed);
        init => _dateAccessed = FileTimeConverter.ToNative(value);
    }

    internal long DateAccessedFileTime
    {
        init => _dateAccessed = value;
    }

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
    public DateTimeOffset? DateRun
    {
        get => FileTimeConverter.FromNative(_dateRun);
        init => _dateRun = FileTimeConverter.ToNative(value);
    }

    internal long DateRunFileTime
    {
        init => _dateRun = value;
    }

    /// <summary>
    /// Gets the recently changed time when requested and available.
    /// </summary>
    public DateTimeOffset? DateRecentlyChanged
    {
        get => FileTimeConverter.FromNative(_dateRecentlyChanged);
        init => _dateRecentlyChanged = FileTimeConverter.ToNative(value);
    }

    internal long DateRecentlyChangedFileTime
    {
        init => _dateRecentlyChanged = value;
    }

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
