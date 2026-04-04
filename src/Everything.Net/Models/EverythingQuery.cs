using Everything.Net.Enums;

namespace Everything.Net.Models;

public sealed class EverythingQuery
{
    public string SearchText { get; init; } = string.Empty;

    public uint Offset { get; init; } = 0;

    public uint MaxResults { get; init; } = 100;

    public EverythingSort? Sort { get; init; }

    public EverythingRequestFlags RequestFlags { get; init; } =
        EverythingRequestFlags.FileName |
        EverythingRequestFlags.Path;

    public bool WaitForResults { get; init; } = true;

    public bool MatchPath { get; init; }

    public bool MatchCase { get; init; }

    public bool MatchWholeWord { get; init; }

    public bool Regex { get; init; }

    public static EverythingQuery Default(string searchText) => new()
    {
        SearchText = searchText
    };
}
