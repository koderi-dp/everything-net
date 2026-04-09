using Everything.Net.Enums;

namespace Everything.Net.Internal;

internal interface IEverythingNativeAdapter
{
    string ExpectedDllName { get; }
    bool IsDbLoaded();
    void SetSearch(string value);
    void SetRequestFlags(EverythingRequestFlags flags);
    void SetSort(uint sortType);
    void SetOffset(uint offset);
    void SetMax(uint max);
    void SetMatchPath(bool enable);
    void SetMatchCase(bool enable);
    void SetMatchWholeWord(bool enable);
    void SetRegex(bool enable);
    bool Query(bool wait);
    uint GetNumResults();
    uint GetTotResults();
    EverythingErrorCode GetLastError();
    string GetResultFullPath(uint index);
    string GetResultFileName(uint index);
    string GetResultPath(uint index);
    string GetResultExtension(uint index);
    bool TryGetResultSize(uint index, out long size);
    bool TryGetResultDateCreated(uint index, out long fileTime);
    bool TryGetResultDateModified(uint index, out long fileTime);
    bool TryGetResultDateAccessed(uint index, out long fileTime);
    uint GetResultAttributes(uint index);
    string GetResultFileListFileName(uint index);
    uint GetResultRunCount(uint index);
    bool TryGetResultDateRun(uint index, out long fileTime);
    bool TryGetResultDateRecentlyChanged(uint index, out long fileTime);
    string GetResultHighlightedFileName(uint index);
    string GetResultHighlightedPath(uint index);
    string GetResultHighlightedFullPath(uint index);
    bool IsFolderResult(uint index);
}
