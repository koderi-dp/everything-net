using Everything.Net.Configuration;
using Everything.Net.Enums;
using Everything.Net.Exceptions;
using Everything.Net.Internal;
using Everything.Net.Models;
using Everything.Net.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Everything.Net.Tests;

public sealed class EverythingClientTests
{
    [Fact]
    public void IsAvailable_ReturnsFalse_WhenNativeAdapterThrowsDllNotFound()
    {
        var native = new FakeEverythingNativeAdapter
        {
            IsDbLoadedException = new DllNotFoundException()
        };
        var client = CreateClient(native);

        Assert.False(client.IsAvailable());
    }

    [Fact]
    public void Search_ReturnsMappedResults_WhenQuerySucceeds()
    {
        var created = DateTimeOffset.UtcNow.AddDays(-2);
        var modified = DateTimeOffset.UtcNow.AddDays(-1);
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = true,
            LastError = EverythingErrorCode.Ok,
            NumResults = 1,
            TotResults = 0,
            ResultFileNames = { [0] = "file.txt" },
            ResultPaths = { [0] = @"C:\data" },
            ResultFullPaths = { [0] = string.Empty },
            ResultExtensions = { [0] = ".txt" },
            ResultSizes = { [0] = 123L },
            ResultDateCreated = { [0] = created.ToFileTime() },
            ResultDateModified = { [0] = modified.ToFileTime() },
            ResultAttributes = { [0] = 32U },
            ResultHighlightedFileNames = { [0] = "<b>file</b>.txt" },
            ResultHighlightedPaths = { [0] = @"C:\<b>data</b>" },
            ResultHighlightedFullPaths = { [0] = @"C:\<b>data</b>\<b>file</b>.txt" },
            ResultIsFolder = { [0] = false }
        };
        var client = CreateClient(native);

        var response = client.Search(new EverythingQuery
        {
            SearchText = "file",
            RequestFlags =
                EverythingRequestFlags.Extension |
                EverythingRequestFlags.Size |
                EverythingRequestFlags.DateCreated |
                EverythingRequestFlags.DateModified |
                EverythingRequestFlags.Attributes |
                EverythingRequestFlags.HighlightedFileName |
                EverythingRequestFlags.HighlightedPath |
                EverythingRequestFlags.HighlightedFullPathAndFileName
        });

        var result = Assert.Single(response.Results);
        Assert.True(response.Success);
        Assert.Equal((uint)1, response.TotalResults);
        Assert.Equal("file.txt", result.FileName);
        Assert.Equal(@"C:\data", result.Path);
        Assert.Equal(@"C:\data\file.txt", result.FullPath);
        Assert.Equal(".txt", result.Extension);
        Assert.Equal(123L, result.Size);
        Assert.Equal(created, result.DateCreated);
        Assert.Equal(modified, result.DateModified);
        Assert.Equal((uint)32, result.Attributes);
        Assert.Equal("<b>file</b>.txt", result.HighlightedFileName);
        Assert.Equal(@"C:\<b>data</b>", result.HighlightedPath);
        Assert.Equal(@"C:\<b>data</b>\<b>file</b>.txt", result.HighlightedFullPath);
        Assert.False(result.IsFolder);
        Assert.True(result.IsFile);
        Assert.Equal(
            EverythingRequestFlags.Extension |
            EverythingRequestFlags.Size |
            EverythingRequestFlags.DateCreated |
            EverythingRequestFlags.DateModified |
            EverythingRequestFlags.Attributes |
            EverythingRequestFlags.HighlightedFileName |
            EverythingRequestFlags.HighlightedPath |
            EverythingRequestFlags.HighlightedFullPathAndFileName |
            EverythingRequestFlags.FileName |
            EverythingRequestFlags.Path,
            native.RequestFlags);
    }

    [Fact]
    public void Search_ThrowsEverythingException_WhenIpcFails_AndThrowOnUnavailableClientIsTrue()
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = false,
            LastError = EverythingErrorCode.Ipc
        };
        var client = CreateClient(native, new EverythingClientOptions
        {
            ThrowOnUnavailableClient = true
        });

        var exception = Assert.Throws<EverythingException>(() => client.Search(EverythingQuery.Default("invoice")));

        Assert.Equal(EverythingErrorCode.Ipc, exception.ErrorCode);
    }

    [Fact]
    public void Search_ReturnsFailureResponse_WhenIpcFails_AndThrowOnUnavailableClientIsFalse()
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = false,
            LastError = EverythingErrorCode.Ipc
        };
        var client = CreateClient(native, new EverythingClientOptions
        {
            ThrowOnUnavailableClient = false
        });

        var response = client.Search(EverythingQuery.Default("invoice"));

        Assert.False(response.Success);
        Assert.Equal(EverythingErrorCode.Ipc, response.ErrorCode);
        Assert.Empty(response.Results);
        Assert.Equal((uint)0, response.TotalResults);
    }

    [Fact]
    public void Search_ReturnsUnavailableResponse_WhenNativeDllIsMissing_AndThrowOnUnavailableClientIsFalse()
    {
        var native = new FakeEverythingNativeAdapter
        {
            SetSearchException = new DllNotFoundException(),
            ExpectedDllName = "Everything64.dll"
        };
        var client = CreateClient(native, new EverythingClientOptions
        {
            ThrowOnUnavailableClient = false
        });

        var response = client.Search(EverythingQuery.Default("invoice"));

        Assert.False(response.Success);
        Assert.Equal(EverythingErrorCode.Ipc, response.ErrorCode);
        Assert.Contains("Expected native DLL: Everything64.dll", response.ErrorMessage);
    }

    [Fact]
    public void Search_ConfiguresNativeQuery_UsingClientDefaults_WhenQueryLeavesValuesUnset()
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = true,
            LastError = EverythingErrorCode.Ok,
            NumResults = 0,
            TotResults = 0
        };
        var client = CreateClient(native, new EverythingClientOptions
        {
            DefaultMaxResults = 250,
            DefaultOffset = 10,
            RequestPathAndFileNameByDefault = false
        });

        var response = client.Search(new EverythingQuery
        {
            SearchText = "abc",
            MatchPath = true,
            MatchCase = true,
            MatchWholeWord = true,
            Regex = true,
            Sort = EverythingSort.DateModifiedDescending
        });

        Assert.True(response.Success);
        Assert.Equal("abc", native.SearchText);
        Assert.Equal((uint)250, native.Max);
        Assert.Equal((uint)10, native.Offset);
        Assert.True(native.MatchPath);
        Assert.True(native.MatchCase);
        Assert.True(native.MatchWholeWord);
        Assert.True(native.Regex);
        Assert.Equal((uint)EverythingSort.DateModifiedDescending, native.Sort);
        Assert.Equal(EverythingRequestFlags.FileName | EverythingRequestFlags.Path, native.RequestFlags);
    }

    private static EverythingClient CreateClient(
        IEverythingNativeAdapter native,
        EverythingClientOptions? options = null)
    {
        return new EverythingClient(
            Options.Create(options ?? new EverythingClientOptions()),
            native);
    }

    private sealed class FakeEverythingNativeAdapter : IEverythingNativeAdapter
    {
        public string ExpectedDllName { get; set; } = "Everything64.dll";
        public Exception? IsDbLoadedException { get; set; }
        public Exception? SetSearchException { get; set; }
        public bool QueryResult { get; set; }
        public EverythingErrorCode LastError { get; set; }
        public uint NumResults { get; set; }
        public uint TotResults { get; set; }
        public string SearchText { get; private set; } = string.Empty;
        public EverythingRequestFlags RequestFlags { get; private set; }
        public uint Sort { get; private set; }
        public uint Offset { get; private set; }
        public uint Max { get; private set; }
        public bool MatchPath { get; private set; }
        public bool MatchCase { get; private set; }
        public bool MatchWholeWord { get; private set; }
        public bool Regex { get; private set; }

        public Dictionary<uint, string> ResultFullPaths { get; } = [];
        public Dictionary<uint, string> ResultFileNames { get; } = [];
        public Dictionary<uint, string> ResultPaths { get; } = [];
        public Dictionary<uint, string> ResultExtensions { get; } = [];
        public Dictionary<uint, long> ResultSizes { get; } = [];
        public Dictionary<uint, long> ResultDateCreated { get; } = [];
        public Dictionary<uint, long> ResultDateModified { get; } = [];
        public Dictionary<uint, long> ResultDateAccessed { get; } = [];
        public Dictionary<uint, uint> ResultAttributes { get; } = [];
        public Dictionary<uint, string> ResultFileListFileNames { get; } = [];
        public Dictionary<uint, uint> ResultRunCounts { get; } = [];
        public Dictionary<uint, long> ResultDateRun { get; } = [];
        public Dictionary<uint, long> ResultDateRecentlyChanged { get; } = [];
        public Dictionary<uint, string> ResultHighlightedFileNames { get; } = [];
        public Dictionary<uint, string> ResultHighlightedPaths { get; } = [];
        public Dictionary<uint, string> ResultHighlightedFullPaths { get; } = [];
        public Dictionary<uint, bool> ResultIsFolder { get; } = [];

        public bool IsDbLoaded()
        {
            if (IsDbLoadedException is not null)
            {
                throw IsDbLoadedException;
            }

            return true;
        }

        public void SetSearch(string value)
        {
            if (SetSearchException is not null)
            {
                throw SetSearchException;
            }

            SearchText = value;
        }

        public void SetRequestFlags(EverythingRequestFlags flags) => RequestFlags = flags;
        public void SetSort(uint sortType) => Sort = sortType;
        public void SetOffset(uint offset) => Offset = offset;
        public void SetMax(uint max) => Max = max;
        public void SetMatchPath(bool enable) => MatchPath = enable;
        public void SetMatchCase(bool enable) => MatchCase = enable;
        public void SetMatchWholeWord(bool enable) => MatchWholeWord = enable;
        public void SetRegex(bool enable) => Regex = enable;
        public bool Query(bool wait) => QueryResult;
        public uint GetNumResults() => NumResults;
        public uint GetTotResults() => TotResults;
        public EverythingErrorCode GetLastError() => LastError;
        public string GetResultFullPath(uint index) => ResultFullPaths.GetValueOrDefault(index, string.Empty);
        public string GetResultFileName(uint index) => ResultFileNames.GetValueOrDefault(index, string.Empty);
        public string GetResultPath(uint index) => ResultPaths.GetValueOrDefault(index, string.Empty);
        public string GetResultExtension(uint index) => ResultExtensions.GetValueOrDefault(index, string.Empty);
        public bool TryGetResultSize(uint index, out long size) => ResultSizes.TryGetValue(index, out size);
        public bool TryGetResultDateCreated(uint index, out long fileTime) => ResultDateCreated.TryGetValue(index, out fileTime);
        public bool TryGetResultDateModified(uint index, out long fileTime) => ResultDateModified.TryGetValue(index, out fileTime);
        public bool TryGetResultDateAccessed(uint index, out long fileTime) => ResultDateAccessed.TryGetValue(index, out fileTime);
        public uint GetResultAttributes(uint index) => ResultAttributes.GetValueOrDefault(index, 0U);
        public string GetResultFileListFileName(uint index) => ResultFileListFileNames.GetValueOrDefault(index, string.Empty);
        public uint GetResultRunCount(uint index) => ResultRunCounts.GetValueOrDefault(index, 0U);
        public bool TryGetResultDateRun(uint index, out long fileTime) => ResultDateRun.TryGetValue(index, out fileTime);
        public bool TryGetResultDateRecentlyChanged(uint index, out long fileTime) => ResultDateRecentlyChanged.TryGetValue(index, out fileTime);
        public string GetResultHighlightedFileName(uint index) => ResultHighlightedFileNames.GetValueOrDefault(index, string.Empty);
        public string GetResultHighlightedPath(uint index) => ResultHighlightedPaths.GetValueOrDefault(index, string.Empty);
        public string GetResultHighlightedFullPath(uint index) => ResultHighlightedFullPaths.GetValueOrDefault(index, string.Empty);
        public bool IsFolderResult(uint index) => ResultIsFolder.GetValueOrDefault(index, false);
    }
}
