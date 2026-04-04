using Everything.Net.Enums;

namespace Everything.Net.Search.Services;

internal static class SearchSortParser
{
    private static readonly EverythingSort[] CycleSorts =
    [
        EverythingSort.DateModifiedDescending,
        EverythingSort.NameAscending,
        EverythingSort.PathAscending,
        EverythingSort.SizeDescending,
        EverythingSort.ExtensionAscending,
        EverythingSort.DateCreatedDescending,
        EverythingSort.DateAccessedDescending
    ];

    private static readonly IReadOnlyDictionary<string, EverythingSort> SortAliases =
        new Dictionary<string, EverythingSort>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = EverythingSort.NameAscending,
            ["name-desc"] = EverythingSort.NameDescending,
            ["path"] = EverythingSort.PathAscending,
            ["path-desc"] = EverythingSort.PathDescending,
            ["size"] = EverythingSort.SizeDescending,
            ["size-asc"] = EverythingSort.SizeAscending,
            ["size-desc"] = EverythingSort.SizeDescending,
            ["modified"] = EverythingSort.DateModifiedDescending,
            ["modified-asc"] = EverythingSort.DateModifiedAscending,
            ["modified-desc"] = EverythingSort.DateModifiedDescending,
            ["created"] = EverythingSort.DateCreatedDescending,
            ["created-asc"] = EverythingSort.DateCreatedAscending,
            ["created-desc"] = EverythingSort.DateCreatedDescending,
            ["accessed"] = EverythingSort.DateAccessedDescending,
            ["accessed-asc"] = EverythingSort.DateAccessedAscending,
            ["accessed-desc"] = EverythingSort.DateAccessedDescending,
            ["extension"] = EverythingSort.ExtensionAscending,
            ["extension-desc"] = EverythingSort.ExtensionDescending,
            ["run-count"] = EverythingSort.RunCountDescending,
            ["recent"] = EverythingSort.DateRecentlyChangedDescending
        };

    public static EverythingSort? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return SortAliases.TryGetValue(value.Trim(), out var sort)
            ? sort
            : throw new ArgumentException($"Unsupported sort '{value}'.");
    }

    public static EverythingSort Next(EverythingSort? current)
    {
        if (current is null)
        {
            return CycleSorts[0];
        }

        var index = Array.IndexOf(CycleSorts, current.Value);
        return index < 0 || index == CycleSorts.Length - 1
            ? CycleSorts[0]
            : CycleSorts[index + 1];
    }

    public static string Display(EverythingSort? sort) => sort switch
    {
        EverythingSort.NameAscending => "name",
        EverythingSort.NameDescending => "name-desc",
        EverythingSort.PathAscending => "path",
        EverythingSort.PathDescending => "path-desc",
        EverythingSort.SizeAscending => "size-asc",
        EverythingSort.SizeDescending => "size",
        EverythingSort.ExtensionAscending => "extension",
        EverythingSort.ExtensionDescending => "extension-desc",
        EverythingSort.DateCreatedAscending => "created-asc",
        EverythingSort.DateCreatedDescending => "created",
        EverythingSort.DateModifiedAscending => "modified-asc",
        EverythingSort.DateModifiedDescending => "modified",
        EverythingSort.DateAccessedAscending => "accessed-asc",
        EverythingSort.DateAccessedDescending => "accessed",
        EverythingSort.RunCountDescending => "run-count",
        EverythingSort.DateRecentlyChangedDescending => "recent",
        _ => "default"
    };
}
