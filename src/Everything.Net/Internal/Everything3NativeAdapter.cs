using System.Buffers;
using System.Runtime.InteropServices;
using Everything.Net.Enums;

namespace Everything.Net.Internal;

/// <summary>
/// Drives Everything SDK 3 through the same step-by-step interface as SDK 2: a search state is
/// built up by the setters, <see cref="Query"/> runs it, the getters read the result list, and
/// <see cref="Reset"/> frees both. Callers hold the client's lock for the whole sequence.
/// </summary>
internal sealed unsafe class Everything3NativeAdapter : IEverythingNativeAdapter
{
    private const int StackBufferLength = 512;

    // Everything 1.5 alpha runs as a named instance; released builds use the unnamed one.
    private static readonly string?[] DefaultInstanceNames = [null, "1.5a"];

    private readonly string?[] _instanceNames;
    private IntPtr _client;
    private IntPtr _searchState;
    private IntPtr _resultList;
    private uint _lastError;

    public Everything3NativeAdapter(string? instanceName)
    {
        _instanceNames = string.IsNullOrWhiteSpace(instanceName) ? DefaultInstanceNames : [instanceName];
    }

    ~Everything3NativeAdapter()
    {
        if (_client != IntPtr.Zero)
        {
            Everything3Native.DestroyClient(_client);
        }
    }

    public string ExpectedDllName => NativeLibraryResolver.Sdk3FileName;

    public EverythingSdkVersion SdkVersion => EverythingSdkVersion.V3;

    public string? LoadedLibraryPath => EverythingNativeLibraryLocator.GetLoadedPath(NativeLibraryResolver.Sdk3FileName);

    /// <summary>Opens the pipe to Everything if it is not open already.</summary>
    public bool TryConnect()
    {
        if (_client != IntPtr.Zero)
        {
            return true;
        }

        foreach (var instanceName in _instanceNames)
        {
            _client = Everything3Native.Connect(instanceName);
            if (_client != IntPtr.Zero)
            {
                _lastError = 0;
                return true;
            }

            _lastError = (uint)Marshal.GetLastPInvokeError();
        }

        return false;
    }

    public bool IsDbLoaded()
    {
        if (!TryConnect())
        {
            return false;
        }

        if (Everything3Native.IsDbLoaded(_client) != 0)
        {
            return true;
        }

        _lastError = (uint)Marshal.GetLastPInvokeError();
        DisconnectIfBroken();
        return false;
    }

    public void SetSearch(string value)
    {
        DestroySearchState();
        _searchState = Everything3Native.CreateSearchState();
        Everything3Native.SetSearchText(_searchState, value);
    }

    public void SetRequestFlags(EverythingRequestFlags flags)
    {
        Request(flags, EverythingRequestFlags.FileName, Everything3Native.PropertyName);
        Request(flags, EverythingRequestFlags.Path, Everything3Native.PropertyPath);
        Request(flags, EverythingRequestFlags.FullPathAndFileName, Everything3Native.PropertyFullPath);
        Request(flags, EverythingRequestFlags.Extension, Everything3Native.PropertyExtension);
        Request(flags, EverythingRequestFlags.Size, Everything3Native.PropertySize);
        Request(flags, EverythingRequestFlags.DateCreated, Everything3Native.PropertyDateCreated);
        Request(flags, EverythingRequestFlags.DateModified, Everything3Native.PropertyDateModified);
        Request(flags, EverythingRequestFlags.DateAccessed, Everything3Native.PropertyDateAccessed);
        Request(flags, EverythingRequestFlags.Attributes, Everything3Native.PropertyAttributes);
        Request(flags, EverythingRequestFlags.FileListFileName, Everything3Native.PropertyFileListFullPath);
        Request(flags, EverythingRequestFlags.RunCount, Everything3Native.PropertyRunCount);
        Request(flags, EverythingRequestFlags.DateRun, Everything3Native.PropertyDateRun);
        Request(flags, EverythingRequestFlags.DateRecentlyChanged, Everything3Native.PropertyDateRecentlyChanged);
        RequestHighlighted(flags, EverythingRequestFlags.HighlightedFileName, Everything3Native.PropertyName);
        RequestHighlighted(flags, EverythingRequestFlags.HighlightedPath, Everything3Native.PropertyPath);
        RequestHighlighted(flags, EverythingRequestFlags.HighlightedFullPathAndFileName, Everything3Native.PropertyFullPath);
    }

    public void SetSort(uint sortType)
    {
        if (TryMapSort(sortType, out var propertyId, out var ascending))
        {
            Everything3Native.SetSearchSort(_searchState, propertyId, ascending ? 1 : 0);
        }
    }

    public void SetOffset(uint offset) => Everything3Native.SetSearchViewportOffset(_searchState, offset);

    // In SDK 2 the largest value means "everything"; keep that meaning.
    public void SetMax(uint max) => Everything3Native.SetSearchViewportCount(_searchState, max == uint.MaxValue ? nuint.MaxValue : max);

    public void SetMatchPath(bool enable) => Everything3Native.SetSearchMatchPath(_searchState, enable ? 1 : 0);

    public void SetMatchCase(bool enable) => Everything3Native.SetSearchMatchCase(_searchState, enable ? 1 : 0);

    public void SetMatchWholeWord(bool enable) => Everything3Native.SetSearchMatchWholeWords(_searchState, enable ? 1 : 0);

    public void SetRegex(bool enable) => Everything3Native.SetSearchRegex(_searchState, enable ? 1 : 0);

    public bool Query(bool wait)
    {
        DestroyResultList();

        // A second attempt covers Everything having been restarted since the pipe was opened.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (!TryConnect())
            {
                return false;
            }

            _resultList = Everything3Native.Search(_client, _searchState);
            if (_resultList != IntPtr.Zero)
            {
                _lastError = 0;
                return true;
            }

            _lastError = (uint)Marshal.GetLastPInvokeError();
            if (!DisconnectIfBroken())
            {
                break;
            }
        }

        return false;
    }

    public void Reset()
    {
        DestroyResultList();
        DestroySearchState();
    }

    public uint GetNumResults() => ToUInt32(Everything3Native.GetResultListViewportCount(_resultList));

    public uint GetTotResults() => ToUInt32(Everything3Native.GetResultListCount(_resultList));

    public EverythingErrorCode GetLastError() => _lastError switch
    {
        0 => EverythingErrorCode.Ok,
        Everything3Native.ErrorOutOfMemory => EverythingErrorCode.Memory,
        Everything3Native.ErrorInvalidParameter or
            Everything3Native.ErrorBadRequest or
            Everything3Native.ErrorInvalidCommand => EverythingErrorCode.InvalidCall,
        _ => EverythingErrorCode.Ipc
    };

    public string GetResultFullPath(uint index) => ReadText(index, Everything3Native.PropertyFullPath, highlighted: false, pool: null);

    public string GetResultFileName(uint index) => ReadText(index, Everything3Native.PropertyName, highlighted: false, pool: null);

    public string GetResultPath(uint index, StringPool pool) => ReadText(index, Everything3Native.PropertyPath, highlighted: false, pool);

    public string GetResultExtension(uint index, StringPool pool) => ReadText(index, Everything3Native.PropertyExtension, highlighted: false, pool);

    public bool TryGetResultSize(uint index, out long size) => TryReadInt64(index, Everything3Native.PropertySize, out size);

    public bool TryGetResultDateCreated(uint index, out long fileTime) => TryReadInt64(index, Everything3Native.PropertyDateCreated, out fileTime);

    public bool TryGetResultDateModified(uint index, out long fileTime) => TryReadInt64(index, Everything3Native.PropertyDateModified, out fileTime);

    public bool TryGetResultDateAccessed(uint index, out long fileTime) => TryReadInt64(index, Everything3Native.PropertyDateAccessed, out fileTime);

    public uint GetResultAttributes(uint index)
    {
        const uint Directory = 0x10;

        var attributes = Everything3Native.GetResultPropertyUInt32(_resultList, index, Everything3Native.PropertyAttributes);
        if (attributes == uint.MaxValue)
        {
            attributes = 0;
        }

        // SDK 2 always marks folders as directories, even when Everything has no attributes for them.
        return IsFolderResult(index) ? attributes | Directory : attributes;
    }

    public string GetResultFileListFileName(uint index) => ReadText(index, Everything3Native.PropertyFileListFullPath, highlighted: false, pool: null);

    public uint GetResultRunCount(uint index) => Everything3Native.GetResultPropertyUInt32(_resultList, index, Everything3Native.PropertyRunCount);

    public bool TryGetResultDateRun(uint index, out long fileTime) => TryReadInt64(index, Everything3Native.PropertyDateRun, out fileTime);

    public bool TryGetResultDateRecentlyChanged(uint index, out long fileTime) => TryReadInt64(index, Everything3Native.PropertyDateRecentlyChanged, out fileTime);

    public string GetResultHighlightedFileName(uint index) => ReadText(index, Everything3Native.PropertyName, highlighted: true, pool: null);

    public string GetResultHighlightedPath(uint index) => ReadText(index, Everything3Native.PropertyPath, highlighted: true, pool: null);

    public string GetResultHighlightedFullPath(uint index) => ReadText(index, Everything3Native.PropertyFullPath, highlighted: true, pool: null);

    public bool IsFolderResult(uint index) => Everything3Native.IsFolderResult(_resultList, index) != 0;

    /// <summary>Maps an SDK 2 sort value to the SDK 3 property and direction.</summary>
    internal static bool TryMapSort(uint sortType, out uint propertyId, out bool ascending)
    {
        // SDK 2 numbers its sorts in ascending/descending pairs starting at 1.
        ascending = sortType % 2 == 1;

        uint? property = ((sortType + 1) / 2) switch
        {
            1 => Everything3Native.PropertyName,
            2 => Everything3Native.PropertyPath,
            3 => Everything3Native.PropertySize,
            4 => Everything3Native.PropertyExtension,
            5 => Everything3Native.PropertyType,
            6 => Everything3Native.PropertyDateCreated,
            7 => Everything3Native.PropertyDateModified,
            8 => Everything3Native.PropertyAttributes,
            9 => Everything3Native.PropertyFileListFullPath,
            10 => Everything3Native.PropertyRunCount,
            11 => Everything3Native.PropertyDateRecentlyChanged,
            12 => Everything3Native.PropertyDateAccessed,
            13 => Everything3Native.PropertyDateRun,
            _ => null
        };

        propertyId = property ?? 0;
        return sortType != 0 && property.HasValue;
    }

    private void Request(EverythingRequestFlags flags, EverythingRequestFlags flag, uint propertyId)
    {
        if ((flags & flag) != 0)
        {
            Everything3Native.AddSearchPropertyRequest(_searchState, propertyId);
        }
    }

    private void RequestHighlighted(EverythingRequestFlags flags, EverythingRequestFlags flag, uint propertyId)
    {
        if ((flags & flag) != 0)
        {
            Everything3Native.AddSearchPropertyRequestHighlighted(_searchState, propertyId);
        }
    }

    private bool TryReadInt64(uint index, uint propertyId, out long value)
    {
        // The SDK reports "unknown" as the largest unsigned value.
        var raw = Everything3Native.GetResultPropertyUInt64(_resultList, index, propertyId);
        value = raw == ulong.MaxValue ? 0 : (long)raw;
        return raw != ulong.MaxValue;
    }

    private string ReadText(uint index, uint propertyId, bool highlighted, StringPool? pool)
    {
        var stackBuffer = stackalloc char[StackBufferLength];
        var length = (int)Copy(stackBuffer, StackBufferLength);

        // The SDK truncates to fit, so a completely full buffer may be missing its tail.
        if (length < StackBufferLength - 1)
        {
            return ToString(new ReadOnlySpan<char>(stackBuffer, length), pool);
        }

        var required = (int)Copy(null, 0);
        var rented = ArrayPool<char>.Shared.Rent(required + 2);
        try
        {
            fixed (char* buffer = rented)
            {
                length = (int)Copy(buffer, (nuint)rented.Length);
                return ToString(new ReadOnlySpan<char>(buffer, length), pool);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        nuint Copy(char* buffer, nuint size) => highlighted
            ? Everything3Native.GetResultPropertyTextHighlighted(_resultList, index, propertyId, buffer, size)
            : Everything3Native.GetResultPropertyText(_resultList, index, propertyId, buffer, size);
    }

    private static string ToString(ReadOnlySpan<char> text, StringPool? pool)
    {
        if (text.IsEmpty)
        {
            return string.Empty;
        }

        return pool is null ? new string(text) : pool.GetOrAdd(text);
    }

    /// <summary>Drops the connection after a pipe failure so the next call reconnects.</summary>
    private bool DisconnectIfBroken()
    {
        if (_lastError is not (Everything3Native.ErrorDisconnected or Everything3Native.ErrorPipeNotFound))
        {
            return false;
        }

        DestroyResultList();
        if (_client != IntPtr.Zero)
        {
            Everything3Native.DestroyClient(_client);
            _client = IntPtr.Zero;
        }

        return true;
    }

    private void DestroyResultList()
    {
        if (_resultList != IntPtr.Zero)
        {
            Everything3Native.DestroyResultList(_resultList);
            _resultList = IntPtr.Zero;
        }
    }

    private void DestroySearchState()
    {
        if (_searchState != IntPtr.Zero)
        {
            Everything3Native.DestroySearchState(_searchState);
            _searchState = IntPtr.Zero;
        }
    }

    private static uint ToUInt32(nuint value) => value > uint.MaxValue ? uint.MaxValue : (uint)value;
}
