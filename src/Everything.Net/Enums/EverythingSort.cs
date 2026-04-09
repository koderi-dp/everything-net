namespace Everything.Net.Enums;

/// <summary>
/// Sort orders supported by the Everything SDK.
/// </summary>
public enum EverythingSort : uint
{
    /// <summary>Sort by name ascending.</summary>
    NameAscending = 1,
    /// <summary>Sort by name descending.</summary>
    NameDescending = 2,
    /// <summary>Sort by path ascending.</summary>
    PathAscending = 3,
    /// <summary>Sort by path descending.</summary>
    PathDescending = 4,
    /// <summary>Sort by size ascending.</summary>
    SizeAscending = 5,
    /// <summary>Sort by size descending.</summary>
    SizeDescending = 6,
    /// <summary>Sort by extension ascending.</summary>
    ExtensionAscending = 7,
    /// <summary>Sort by extension descending.</summary>
    ExtensionDescending = 8,
    /// <summary>Sort by type name ascending.</summary>
    TypeNameAscending = 9,
    /// <summary>Sort by type name descending.</summary>
    TypeNameDescending = 10,
    /// <summary>Sort by creation date ascending.</summary>
    DateCreatedAscending = 11,
    /// <summary>Sort by creation date descending.</summary>
    DateCreatedDescending = 12,
    /// <summary>Sort by modified date ascending.</summary>
    DateModifiedAscending = 13,
    /// <summary>Sort by modified date descending.</summary>
    DateModifiedDescending = 14,
    /// <summary>Sort by attributes ascending.</summary>
    AttributesAscending = 15,
    /// <summary>Sort by attributes descending.</summary>
    AttributesDescending = 16,
    /// <summary>Sort by file list file name ascending.</summary>
    FileListFilenameAscending = 17,
    /// <summary>Sort by file list file name descending.</summary>
    FileListFilenameDescending = 18,
    /// <summary>Sort by run count ascending.</summary>
    RunCountAscending = 19,
    /// <summary>Sort by run count descending.</summary>
    RunCountDescending = 20,
    /// <summary>Sort by recently changed date ascending.</summary>
    DateRecentlyChangedAscending = 21,
    /// <summary>Sort by recently changed date descending.</summary>
    DateRecentlyChangedDescending = 22,
    /// <summary>Sort by accessed date ascending.</summary>
    DateAccessedAscending = 23,
    /// <summary>Sort by accessed date descending.</summary>
    DateAccessedDescending = 24,
    /// <summary>Sort by run date ascending.</summary>
    DateRunAscending = 25,
    /// <summary>Sort by run date descending.</summary>
    DateRunDescending = 26
}
