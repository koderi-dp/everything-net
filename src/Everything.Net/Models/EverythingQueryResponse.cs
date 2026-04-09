using Everything.Net.Enums;

namespace Everything.Net.Models;

/// <summary>
/// Represents the outcome of an Everything search request.
/// </summary>
public sealed class EverythingQueryResponse
{
    /// <summary>
    /// Gets the search text that was executed.
    /// </summary>
    public required string SearchText { get; init; }

    /// <summary>
    /// Gets a value indicating whether the query completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the total number of matches reported by Everything.
    /// </summary>
    public required uint TotalResults { get; init; }

    /// <summary>
    /// Gets the visible result window returned for the current request.
    /// </summary>
    public required IReadOnlyList<EverythingSearchResult> Results { get; init; }

    /// <summary>
    /// Gets the Everything error code associated with the request.
    /// </summary>
    public EverythingErrorCode ErrorCode { get; init; }

    /// <summary>
    /// Gets the error message when the request fails.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
