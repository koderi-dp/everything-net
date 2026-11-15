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

    [Fact]
    public void Search_UsesDefaultSort_WhenQueryHasNoSort_AfterASortedQuery()
    {
        var native = new FakeEverythingNativeAdapter { QueryResult = true };
        var client = CreateClient(native);

        client.Search(new EverythingQuery { SearchText = "a", Sort = EverythingSort.SizeDescending });
        client.Search(EverythingQuery.Default("b"));

        Assert.Equal((uint)EverythingSort.NameAscending, native.Sort);
    }

    [Fact]
    public void Search_ResetsNativeState_AfterReadingResults()
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = true,
            NumResults = 1,
            ResultFileNames = { [0] = "file.txt" },
            ResultPaths = { [0] = @"C:\data" }
        };
        var client = CreateClient(native);

        var response = client.Search(EverythingQuery.Default("file"));

        Assert.Single(response.Results);
        Assert.Equal(1, native.ResetCount);
        Assert.Equal(0, native.ResultReadsAfterReset);
    }

    [Fact]
    public void Search_ResetsNativeState_WhenQueryFails()
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = false,
            LastError = EverythingErrorCode.Ipc
        };
        var client = CreateClient(native, new EverythingClientOptions { ThrowOnUnavailableClient = false });

        client.Search(EverythingQuery.Default("file"));

        Assert.Equal(1, native.ResetCount);
    }

    [Fact]
    public async Task Search_RunsOneAtATime_WhenCalledConcurrently_AcrossClients()
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = true,
            QueryDelay = TimeSpan.FromMilliseconds(20)
        };
        var clients = new[] { CreateClient(native), CreateClient(native) };

        await Task.WhenAll(Enumerable.Range(0, 8).Select(i =>
            clients[i % 2].SearchAsync(EverythingQuery.Default("file"))));

        Assert.Equal(8, native.ResetCount);
        Assert.Equal(1, native.MaxConcurrentSearches);
    }

    [Theory]
    [InlineData(@"C:\data", "file.txt", @"C:\data\file.txt")]
    [InlineData("C:", "file.txt", @"C:\file.txt")]
    [InlineData("", "C:", "C:")]
    [InlineData(@"\server\share", "file.txt", @"\server\share\file.txt")]
    [InlineData(@"\server", "share", @"\server\share")]
    public void Search_BuildsFullPathFromPathAndFileName(string path, string fileName, string expected)
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = true,
            NumResults = 1,
            ResultFileNames = { [0] = fileName },
            ResultPaths = { [0] = path },
            ResultFullPaths = { [0] = "not used when name and path are requested" }
        };
        var client = CreateClient(native);

        var result = Assert.Single(client.Search(EverythingQuery.Default("file")).Results);

        Assert.Equal(expected, result.FullPath);
        Assert.Same(result.FullPath, result.FullPath);
    }

    [Fact]
    public void Search_UsesNativeFullPath_WhenPathAndFileNameAreNotRequested()
    {
        var native = new FakeEverythingNativeAdapter
        {
            QueryResult = true,
            NumResults = 1,
            ResultFullPaths = { [0] = @"C:\data\file.txt" }
        };
        var client = CreateClient(native, new EverythingClientOptions { RequestPathAndFileNameByDefault = false });

        var response = client.Search(new EverythingQuery
        {
            SearchText = "file",
            RequestFlags = EverythingRequestFlags.FullPathAndFileName
        });

        Assert.Equal(@"C:\data\file.txt", Assert.Single(response.Results).FullPath);
    }

    [Fact]
    public void SearchResult_KeepsValues_WhenBuiltByHand()
    {
        var created = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));

        var result = new EverythingSearchResult
        {
            Index = 0,
            FileName = "file.txt",
            Path = @"C:\data",
            FullPath = @"D:\elsewhere\file.txt",
            DateCreated = created,
            DateModified = DateTimeOffset.MinValue
        };

        Assert.Equal(@"D:\elsewhere\file.txt", result.FullPath);
        Assert.Equal(created, result.DateCreated);
        Assert.Null(result.DateModified);
        Assert.Null(result.DateAccessed);
    }

    [Fact]
    public void StringPool_ReturnsTheSameInstance_ForEqualValues()
    {
        var pool = new StringPool();

        var first = pool.GetOrAdd(@"C:\data");
        var other = pool.GetOrAdd(@"C:\other");
        var again = pool.GetOrAdd(new string(@"C:\data"));

        Assert.Same(first, again);
        Assert.NotSame(first, other);
        Assert.Same(string.Empty, pool.GetOrAdd(""));
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
        public TimeSpan QueryDelay { get; set; }
        public int ResetCount { get; private set; }
        public int ResultReadsAfterReset { get; private set; }
        public int MaxConcurrentSearches { get; private set; }

        private int _activeSearches;

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

            var active = Interlocked.Increment(ref _activeSearches);
            MaxConcurrentSearches = Math.Max(MaxConcurrentSearches, active);
        }

        public void Reset()
        {
            ResetCount++;
            Interlocked.Decrement(ref _activeSearches);
        }

        public void SetRequestFlags(EverythingRequestFlags flags) => RequestFlags = flags;
        public void SetSort(uint sortType) => Sort = sortType;
        public void SetOffset(uint offset) => Offset = offset;
        public void SetMax(uint max) => Max = max;
        public void SetMatchPath(bool enable) => MatchPath = enable;
        public void SetMatchCase(bool enable) => MatchCase = enable;
        public void SetMatchWholeWord(bool enable) => MatchWholeWord = enable;
        public void SetRegex(bool enable) => Regex = enable;
        public bool Query(bool wait)
        {
            if (QueryDelay > TimeSpan.Zero)
            {
                Thread.Sleep(QueryDelay);
            }

            return QueryResult;
        }
        public uint GetNumResults() => NumResults;
        public uint GetTotResults() => TotResults;
        public EverythingErrorCode GetLastError() => LastError;
        public string GetResultFullPath(uint index) => ResultFullPaths.GetValueOrDefault(index, string.Empty);
        public string GetResultFileName(uint index)
        {
            if (ResetCount > 0)
            {
                ResultReadsAfterReset++;
            }

            return ResultFileNames.GetValueOrDefault(index, string.Empty);
        }
        public string GetResultPath(uint index, StringPool pool) => ResultPaths.GetValueOrDefault(index, string.Empty);
        public string GetResultExtension(uint index, StringPool pool) => ResultExtensions.GetValueOrDefault(index, string.Empty);
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
