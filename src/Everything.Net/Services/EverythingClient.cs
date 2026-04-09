using Everything.Net.Abstractions;
using Everything.Net.Configuration;
using Everything.Net.Enums;
using Everything.Net.Exceptions;
using Everything.Net.Internal;
using Everything.Net.Models;
using Microsoft.Extensions.Options;

namespace Everything.Net.Services;

/// <summary>
/// Default implementation of <see cref="IEverythingClient"/> backed by the native Everything SDK.
/// </summary>
public sealed class EverythingClient : IEverythingClient
{
    private readonly EverythingClientOptions _options;
    private readonly IEverythingNativeAdapter _native;

    public EverythingClient(IOptions<EverythingClientOptions> options)
        : this(options, new EverythingNativeAdapter())
    {
    }

    internal EverythingClient(IOptions<EverythingClientOptions> options, IEverythingNativeAdapter native)
    {
        _options = options.Value;
        _native = native;
    }

    /// <inheritdoc />
    public bool IsAvailable()
    {
        try
        {
            return _native.IsDbLoaded();
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public EverythingQueryResponse Search(EverythingQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            Configure(query);

            var success = _native.Query(query.WaitForResults);
            var errorCode = _native.GetLastError();

            if (!success)
            {
                return HandleFailure(query.SearchText, errorCode);
            }

            var visibleCount = _native.GetNumResults();
            var totalMatches = _native.GetTotResults();
            // Tot can be 0 in edge cases before max is applied; fall back to visible count.
            if (totalMatches == 0 && visibleCount > 0)
            {
                totalMatches = visibleCount;
            }

            var results = ReadResults(visibleCount, query.RequestFlags);

            return new EverythingQueryResponse
            {
                SearchText = query.SearchText,
                Success = true,
                TotalResults = totalMatches,
                Results = results,
                ErrorCode = EverythingErrorCode.Ok,
                ErrorMessage = null
            };
        }
        catch (DllNotFoundException ex)
        {
            return HandleUnavailable(query.SearchText, ex);
        }
        catch (EntryPointNotFoundException ex)
        {
            return HandleUnavailable(query.SearchText, ex);
        }
        catch (PlatformNotSupportedException ex)
        {
            return HandleUnavailable(query.SearchText, ex);
        }
    }

    /// <inheritdoc />
    public Task<EverythingQueryResponse> SearchAsync(
        EverythingQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Search(query);
        }, cancellationToken);
    }

    private void Configure(EverythingQuery query)
    {
        var requestFlags = query.RequestFlags;

        if (_options.RequestPathAndFileNameByDefault)
        {
            requestFlags |= EverythingRequestFlags.FileName | EverythingRequestFlags.Path;
        }

        _native.SetSearch(query.SearchText);
        _native.SetRequestFlags(requestFlags);
        _native.SetOffset(query.Offset == 0 ? _options.DefaultOffset : query.Offset);
        _native.SetMax(query.MaxResults == 0 ? _options.DefaultMaxResults : query.MaxResults);
        _native.SetMatchPath(query.MatchPath);
        _native.SetMatchCase(query.MatchCase);
        _native.SetMatchWholeWord(query.MatchWholeWord);
        _native.SetRegex(query.Regex);

        if (query.Sort.HasValue)
        {
            _native.SetSort((uint)query.Sort.Value);
        }
    }

    private IReadOnlyList<EverythingSearchResult> ReadResults(uint total, EverythingRequestFlags requestFlags)
    {
        var list = new List<EverythingSearchResult>((int)total);

        for (uint i = 0; i < total; i++)
        {
            var fileName = _native.GetResultFileName(i);
            var path = _native.GetResultPath(i);
            var fullPath = _native.GetResultFullPath(i);

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                fullPath = string.IsNullOrWhiteSpace(path)
                    ? fileName
                    : Path.Combine(path, fileName);
            }

            var isFolder = _native.IsFolderResult(i);

            list.Add(new EverythingSearchResult
            {
                Index = i,
                FileName = fileName,
                Path = path,
                FullPath = fullPath,
                Extension = requestFlags.HasFlag(EverythingRequestFlags.Extension) ? NullIfEmpty(_native.GetResultExtension(i)) : null,
                Size = requestFlags.HasFlag(EverythingRequestFlags.Size) && _native.TryGetResultSize(i, out var size) ? size : null,
                DateCreated = requestFlags.HasFlag(EverythingRequestFlags.DateCreated) && _native.TryGetResultDateCreated(i, out var dc) ? FileTimeConverter.FromNative(dc) : null,
                DateModified = requestFlags.HasFlag(EverythingRequestFlags.DateModified) && _native.TryGetResultDateModified(i, out var dm) ? FileTimeConverter.FromNative(dm) : null,
                DateAccessed = requestFlags.HasFlag(EverythingRequestFlags.DateAccessed) && _native.TryGetResultDateAccessed(i, out var da) ? FileTimeConverter.FromNative(da) : null,
                Attributes = requestFlags.HasFlag(EverythingRequestFlags.Attributes) ? _native.GetResultAttributes(i) : null,
                FileListFileName = requestFlags.HasFlag(EverythingRequestFlags.FileListFileName) ? NullIfEmpty(_native.GetResultFileListFileName(i)) : null,
                RunCount = requestFlags.HasFlag(EverythingRequestFlags.RunCount) ? _native.GetResultRunCount(i) : null,
                DateRun = requestFlags.HasFlag(EverythingRequestFlags.DateRun) && _native.TryGetResultDateRun(i, out var dr) ? FileTimeConverter.FromNative(dr) : null,
                DateRecentlyChanged = requestFlags.HasFlag(EverythingRequestFlags.DateRecentlyChanged) && _native.TryGetResultDateRecentlyChanged(i, out var rc) ? FileTimeConverter.FromNative(rc) : null,
                HighlightedFileName = requestFlags.HasFlag(EverythingRequestFlags.HighlightedFileName) ? NullIfEmpty(_native.GetResultHighlightedFileName(i)) : null,
                HighlightedPath = requestFlags.HasFlag(EverythingRequestFlags.HighlightedPath) ? NullIfEmpty(_native.GetResultHighlightedPath(i)) : null,
                HighlightedFullPath = requestFlags.HasFlag(EverythingRequestFlags.HighlightedFullPathAndFileName) ? NullIfEmpty(_native.GetResultHighlightedFullPath(i)) : null,
                IsFolder = isFolder
            });
        }

        return list;
    }

    private EverythingQueryResponse HandleFailure(string searchText, EverythingErrorCode errorCode)
    {
        var message = EverythingErrorHelper.ToMessage(errorCode);

        if (_options.ThrowOnUnavailableClient || errorCode != EverythingErrorCode.Ipc)
        {
            throw new EverythingException(message, errorCode);
        }

        return new EverythingQueryResponse
        {
            SearchText = searchText,
            Success = false,
            TotalResults = 0,
            Results = Array.Empty<EverythingSearchResult>(),
            ErrorCode = errorCode,
            ErrorMessage = message
        };
    }

    private EverythingQueryResponse HandleUnavailable(string searchText, Exception ex)
    {
        var message = $"Everything native DLL was not found, is incompatible, or the current process architecture is unsupported. Expected native DLL: {_native.ExpectedDllName}";

        if (_options.ThrowOnUnavailableClient)
        {
            throw new InvalidOperationException(message, ex);
        }

        return new EverythingQueryResponse
        {
            SearchText = searchText,
            Success = false,
            TotalResults = 0,
            Results = Array.Empty<EverythingSearchResult>(),
            ErrorCode = EverythingErrorCode.Ipc,
            ErrorMessage = message
        };
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
