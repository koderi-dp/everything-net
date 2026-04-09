namespace Everything.Net.Enums;

/// <summary>
/// Specifies which result fields should be populated by Everything.
/// </summary>
[Flags]
public enum EverythingRequestFlags : uint
{
    /// <summary>
    /// Request the file or folder name.
    /// </summary>
    FileName = 0x00000001,

    /// <summary>
    /// Request the parent path.
    /// </summary>
    Path = 0x00000002,

    /// <summary>
    /// Request the full path and file name.
    /// </summary>
    FullPathAndFileName = 0x00000004,

    /// <summary>
    /// Request the file extension.
    /// </summary>
    Extension = 0x00000008,

    /// <summary>
    /// Request the file size.
    /// </summary>
    Size = 0x00000010,

    /// <summary>
    /// Request the creation date.
    /// </summary>
    DateCreated = 0x00000020,

    /// <summary>
    /// Request the modified date.
    /// </summary>
    DateModified = 0x00000040,

    /// <summary>
    /// Request the accessed date.
    /// </summary>
    DateAccessed = 0x00000080,

    /// <summary>
    /// Request native file attributes.
    /// </summary>
    Attributes = 0x00000100,

    /// <summary>
    /// Request the file list file name.
    /// </summary>
    FileListFileName = 0x00000200,

    /// <summary>
    /// Request the run count.
    /// </summary>
    RunCount = 0x00000400,

    /// <summary>
    /// Request the last run date.
    /// </summary>
    DateRun = 0x00000800,

    /// <summary>
    /// Request the recently changed date.
    /// </summary>
    DateRecentlyChanged = 0x00001000,

    /// <summary>
    /// Request highlighted file name text.
    /// </summary>
    HighlightedFileName = 0x00002000,

    /// <summary>
    /// Request highlighted path text.
    /// </summary>
    HighlightedPath = 0x00004000,

    /// <summary>
    /// Request highlighted full path and file name text.
    /// </summary>
    HighlightedFullPathAndFileName = 0x00008000
}
