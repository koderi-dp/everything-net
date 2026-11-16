using Everything.Net.Enums;

namespace Everything.Net.Internal;

/// <summary>
/// Picks the SDK at the start of every operation: SDK 3 when its DLL is present and Everything 1.5
/// answers on its pipe, otherwise SDK 2. The rest of that operation goes to the same one.
/// </summary>
internal sealed class AutoNativeAdapter : IEverythingNativeAdapter
{
    private readonly IEverythingNativeAdapter _sdk2;
    private readonly IEverythingNativeAdapter _sdk3;
    private readonly Func<bool> _tryConnectSdk3;
    private IEverythingNativeAdapter _current;
    private bool _sdk3Missing;

    public AutoNativeAdapter(string? instanceName)
    {
        var sdk3 = new Everything3NativeAdapter(instanceName);
        _sdk2 = new EverythingNativeAdapter();
        _sdk3 = sdk3;
        _tryConnectSdk3 = sdk3.TryConnect;
        _current = _sdk2;
    }

    internal AutoNativeAdapter(IEverythingNativeAdapter sdk2, IEverythingNativeAdapter sdk3, Func<bool> tryConnectSdk3)
    {
        _sdk2 = sdk2;
        _sdk3 = sdk3;
        _tryConnectSdk3 = tryConnectSdk3;
        _current = sdk2;
    }

    public string ExpectedDllName => _current.ExpectedDllName;
    public EverythingSdkVersion SdkVersion => _current.SdkVersion;
    public string? LoadedLibraryPath => _current.LoadedLibraryPath;

    public bool IsDbLoaded() => Select().IsDbLoaded();
    public void SetSearch(string value) => Select().SetSearch(value);

    public void SetRequestFlags(EverythingRequestFlags flags) => _current.SetRequestFlags(flags);
    public void SetSort(uint sortType) => _current.SetSort(sortType);
    public void SetOffset(uint offset) => _current.SetOffset(offset);
    public void SetMax(uint max) => _current.SetMax(max);
    public void SetMatchPath(bool enable) => _current.SetMatchPath(enable);
    public void SetMatchCase(bool enable) => _current.SetMatchCase(enable);
    public void SetMatchWholeWord(bool enable) => _current.SetMatchWholeWord(enable);
    public void SetRegex(bool enable) => _current.SetRegex(enable);
    public bool Query(bool wait) => _current.Query(wait);
    public void Reset() => _current.Reset();
    public uint GetNumResults() => _current.GetNumResults();
    public uint GetTotResults() => _current.GetTotResults();
    public EverythingErrorCode GetLastError() => _current.GetLastError();
    public string GetResultFullPath(uint index) => _current.GetResultFullPath(index);
    public string GetResultFileName(uint index) => _current.GetResultFileName(index);
    public string GetResultPath(uint index, StringPool pool) => _current.GetResultPath(index, pool);
    public string GetResultExtension(uint index, StringPool pool) => _current.GetResultExtension(index, pool);
    public bool TryGetResultSize(uint index, out long size) => _current.TryGetResultSize(index, out size);
    public bool TryGetResultDateCreated(uint index, out long fileTime) => _current.TryGetResultDateCreated(index, out fileTime);
    public bool TryGetResultDateModified(uint index, out long fileTime) => _current.TryGetResultDateModified(index, out fileTime);
    public bool TryGetResultDateAccessed(uint index, out long fileTime) => _current.TryGetResultDateAccessed(index, out fileTime);
    public uint GetResultAttributes(uint index) => _current.GetResultAttributes(index);
    public string GetResultFileListFileName(uint index) => _current.GetResultFileListFileName(index);
    public uint GetResultRunCount(uint index) => _current.GetResultRunCount(index);
    public bool TryGetResultDateRun(uint index, out long fileTime) => _current.TryGetResultDateRun(index, out fileTime);
    public bool TryGetResultDateRecentlyChanged(uint index, out long fileTime) => _current.TryGetResultDateRecentlyChanged(index, out fileTime);
    public string GetResultHighlightedFileName(uint index) => _current.GetResultHighlightedFileName(index);
    public string GetResultHighlightedPath(uint index) => _current.GetResultHighlightedPath(index);
    public string GetResultHighlightedFullPath(uint index) => _current.GetResultHighlightedFullPath(index);
    public bool IsFolderResult(uint index) => _current.IsFolderResult(index);

    private IEverythingNativeAdapter Select()
    {
        _current = !_sdk3Missing && CanUseSdk3() ? _sdk3 : _sdk2;
        return _current;
    }

    private bool CanUseSdk3()
    {
        try
        {
            return _tryConnectSdk3();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            // The SDK 3 DLL is not deployed (or is unusable); that will not change while the process runs.
            _sdk3Missing = true;
            return false;
        }
    }
}
