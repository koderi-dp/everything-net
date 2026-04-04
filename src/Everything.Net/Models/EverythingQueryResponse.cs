using Everything.Net.Enums;

namespace Everything.Net.Models;

public sealed class EverythingQueryResponse
{
    public required string SearchText { get; init; }

    public required bool Success { get; init; }

    public required uint TotalResults { get; init; }

    public required IReadOnlyList<EverythingSearchResult> Results { get; init; }

    public EverythingErrorCode ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}
