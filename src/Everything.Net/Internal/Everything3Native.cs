using System.Runtime.InteropServices;

namespace Everything.Net.Internal;

/// <summary>
/// P/Invoke declarations for Everything SDK 3 (Everything 1.5, named pipes).
/// BOOL is declared as <see cref="int"/> so every signature stays blittable.
/// </summary>
internal static unsafe class Everything3Native
{
    private const string Lib = NativeLibraryResolver.Sdk3;

    public const uint ErrorOutOfMemory = 0xE0000001;
    public const uint ErrorPipeNotFound = 0xE0000002;
    public const uint ErrorDisconnected = 0xE0000003;
    public const uint ErrorInvalidParameter = 0xE0000004;
    public const uint ErrorBadRequest = 0xE0000005;
    public const uint ErrorInvalidCommand = 0xE0000009;

    public const uint PropertyName = 0;
    public const uint PropertyPath = 1;
    public const uint PropertySize = 2;
    public const uint PropertyExtension = 3;
    public const uint PropertyType = 4;
    public const uint PropertyDateModified = 5;
    public const uint PropertyDateCreated = 6;
    public const uint PropertyDateAccessed = 7;
    public const uint PropertyAttributes = 8;
    public const uint PropertyDateRecentlyChanged = 9;
    public const uint PropertyRunCount = 10;
    public const uint PropertyDateRun = 11;
    public const uint PropertyFullPath = 240;
    public const uint PropertyFileListFullPath = 263;

    static Everything3Native()
    {
        NativeLibraryResolver.EnsureRegistered();
    }

    [DllImport(Lib, EntryPoint = "Everything3_ConnectW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr Connect(string? instanceName);

    [DllImport(Lib, EntryPoint = "Everything3_DestroyClient")]
    public static extern int DestroyClient(IntPtr client);

    [DllImport(Lib, EntryPoint = "Everything3_IsDBLoaded", SetLastError = true)]
    public static extern int IsDbLoaded(IntPtr client);

    [DllImport(Lib, EntryPoint = "Everything3_CreateSearchState")]
    public static extern IntPtr CreateSearchState();

    [DllImport(Lib, EntryPoint = "Everything3_DestroySearchState")]
    public static extern int DestroySearchState(IntPtr searchState);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchTextW", CharSet = CharSet.Unicode)]
    public static extern int SetSearchText(IntPtr searchState, string search);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchMatchCase")]
    public static extern int SetSearchMatchCase(IntPtr searchState, int enable);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchMatchWholeWords")]
    public static extern int SetSearchMatchWholeWords(IntPtr searchState, int enable);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchMatchPath")]
    public static extern int SetSearchMatchPath(IntPtr searchState, int enable);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchRegex")]
    public static extern int SetSearchRegex(IntPtr searchState, int enable);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchSort")]
    public static extern int SetSearchSort(IntPtr searchState, uint propertyId, int ascending);

    [DllImport(Lib, EntryPoint = "Everything3_AddSearchPropertyRequest")]
    public static extern int AddSearchPropertyRequest(IntPtr searchState, uint propertyId);

    [DllImport(Lib, EntryPoint = "Everything3_AddSearchPropertyRequestHighlighted")]
    public static extern int AddSearchPropertyRequestHighlighted(IntPtr searchState, uint propertyId);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchViewportOffset")]
    public static extern int SetSearchViewportOffset(IntPtr searchState, nuint offset);

    [DllImport(Lib, EntryPoint = "Everything3_SetSearchViewportCount")]
    public static extern int SetSearchViewportCount(IntPtr searchState, nuint count);

    [DllImport(Lib, EntryPoint = "Everything3_Search", SetLastError = true)]
    public static extern IntPtr Search(IntPtr client, IntPtr searchState);

    [DllImport(Lib, EntryPoint = "Everything3_DestroyResultList")]
    public static extern int DestroyResultList(IntPtr resultList);

    [DllImport(Lib, EntryPoint = "Everything3_GetResultListCount")]
    public static extern nuint GetResultListCount(IntPtr resultList);

    [DllImport(Lib, EntryPoint = "Everything3_GetResultListViewportCount")]
    public static extern nuint GetResultListViewportCount(IntPtr resultList);

    [DllImport(Lib, EntryPoint = "Everything3_IsFolderResult")]
    public static extern int IsFolderResult(IntPtr resultList, nuint index);

    [DllImport(Lib, EntryPoint = "Everything3_GetResultPropertyTextW")]
    public static extern nuint GetResultPropertyText(IntPtr resultList, nuint index, uint propertyId, char* buffer, nuint bufferSize);

    [DllImport(Lib, EntryPoint = "Everything3_GetResultPropertyTextHighlightedW")]
    public static extern nuint GetResultPropertyTextHighlighted(IntPtr resultList, nuint index, uint propertyId, char* buffer, nuint bufferSize);

    [DllImport(Lib, EntryPoint = "Everything3_GetResultPropertyUINT64")]
    public static extern ulong GetResultPropertyUInt64(IntPtr resultList, nuint index, uint propertyId);

    [DllImport(Lib, EntryPoint = "Everything3_GetResultPropertyDWORD")]
    public static extern uint GetResultPropertyUInt32(IntPtr resultList, nuint index, uint propertyId);
}
