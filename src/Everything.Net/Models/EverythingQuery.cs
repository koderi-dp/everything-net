using Everything.Net.Enums;

namespace Everything.Net.Models;

/// <summary>
/// Describes a search request to execute against Everything.
/// </summary>
public sealed class EverythingQuery
{
    /// <summary>
    /// Gets the raw Everything search text.
    /// </summary>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the zero-based result offset to request from Everything.
    /// </summary>
    public uint Offset { get; init; } = 0;

    /// <summary>
    /// Gets the maximum number of results to request.
    /// </summary>
    public uint MaxResults { get; init; } = 100;

    /// <summary>
    /// Gets the optional sort mode to apply.
    /// </summary>
    public EverythingSort? Sort { get; init; }

    /// <summary>
    /// Gets the result fields to request from Everything.
    /// </summary>
    public EverythingRequestFlags RequestFlags { get; init; } =
        EverythingRequestFlags.FileName |
        EverythingRequestFlags.Path;

    /// <summary>
    /// Gets a value indicating whether the search should wait for results before returning.
    /// </summary>
    public bool WaitForResults { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether path text should participate in matching.
    /// </summary>
    public bool MatchPath { get; init; }

    /// <summary>
    /// Gets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool MatchCase { get; init; }

    /// <summary>
    /// Gets a value indicating whether matches must align to whole words.
    /// </summary>
    public bool MatchWholeWord { get; init; }

    /// <summary>
    /// Gets a value indicating whether the search text should be interpreted as a regular expression.
    /// </summary>
    public bool Regex { get; init; }

    /// <summary>
    /// Creates a query with the default settings for the supplied search text.
    /// </summary>
    /// <param name="searchText">The Everything search text.</param>
    /// <returns>A query initialized with the provided search text.</returns>
    public static EverythingQuery Default(string searchText) => new()
    {
        SearchText = searchText
    };
}
