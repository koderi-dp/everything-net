using Everything.Net.Enums;

namespace Everything.Net.Internal;

internal sealed class EverythingNativeAdapter : IEverythingNativeAdapter
{
    public string ExpectedDllName => EverythingNative.ExpectedDllName;

    public bool IsDbLoaded() => EverythingNative.IsDbLoaded();
    public void SetSearch(string value) => EverythingNative.SetSearch(value);
    public void SetRequestFlags(EverythingRequestFlags flags) => EverythingNative.SetRequestFlags(flags);
    public void SetSort(uint sortType) => EverythingNative.SetSort(sortType);
    public void SetOffset(uint offset) => EverythingNative.SetOffset(offset);
    public void SetMax(uint max) => EverythingNative.SetMax(max);
    public void SetMatchPath(bool enable) => EverythingNative.SetMatchPath(enable);
    public void SetMatchCase(bool enable) => EverythingNative.SetMatchCase(enable);
    public void SetMatchWholeWord(bool enable) => EverythingNative.SetMatchWholeWord(enable);
    public void SetRegex(bool enable) => EverythingNative.SetRegex(enable);
    public bool Query(bool wait) => EverythingNative.Query(wait);
    public uint GetNumResults() => EverythingNative.GetNumResults();
    public uint GetTotResults() => EverythingNative.GetTotResults();
    public EverythingErrorCode GetLastError() => EverythingNative.GetLastError();
    public string GetResultFullPath(uint index) => EverythingNative.GetResultFullPath(index);
    public string GetResultFileName(uint index) => EverythingNative.GetResultFileName(index);
    public string GetResultPath(uint index) => EverythingNative.GetResultPath(index);
    public string GetResultExtension(uint index) => EverythingNative.GetResultExtension(index);
    public bool TryGetResultSize(uint index, out long size) => EverythingNative.TryGetResultSize(index, out size);
    public bool TryGetResultDateCreated(uint index, out long fileTime) => EverythingNative.TryGetResultDateCreated(index, out fileTime);
    public bool TryGetResultDateModified(uint index, out long fileTime) => EverythingNative.TryGetResultDateModified(index, out fileTime);
    public bool TryGetResultDateAccessed(uint index, out long fileTime) => EverythingNative.TryGetResultDateAccessed(index, out fileTime);
    public uint GetResultAttributes(uint index) => EverythingNative.GetResultAttributes(index);
    public string GetResultFileListFileName(uint index) => EverythingNative.GetResultFileListFileName(index);
    public uint GetResultRunCount(uint index) => EverythingNative.GetResultRunCount(index);
    public bool TryGetResultDateRun(uint index, out long fileTime) => EverythingNative.TryGetResultDateRun(index, out fileTime);
    public bool TryGetResultDateRecentlyChanged(uint index, out long fileTime) => EverythingNative.TryGetResultDateRecentlyChanged(index, out fileTime);
    public string GetResultHighlightedFileName(uint index) => EverythingNative.GetResultHighlightedFileName(index);
    public string GetResultHighlightedPath(uint index) => EverythingNative.GetResultHighlightedPath(index);
    public string GetResultHighlightedFullPath(uint index) => EverythingNative.GetResultHighlightedFullPath(index);
    public bool IsFolderResult(uint index) => EverythingNative.IsFolderResult(index);
}
