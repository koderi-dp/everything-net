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
public sealed class EverythingClient(IOptions<EverythingClientOptions> options) : IEverythingClient
{
    private readonly EverythingClientOptions _options = options.Value;

    /// <inheritdoc />
    public bool IsAvailable()
    {
        try
        {
            return EverythingNative.IsDbLoaded();
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

            var success = EverythingNative.Query(query.WaitForResults);
            var errorCode = EverythingNative.GetLastError();

            if (!success)
            {
                return HandleFailure(query.SearchText, errorCode);
            }

            var visibleCount = EverythingNative.GetNumResults();
            var totalMatches = EverythingNative.GetTotResults();
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

        EverythingNative.SetSearch(query.SearchText);
        EverythingNative.SetRequestFlags(requestFlags);
        EverythingNative.SetOffset(query.Offset == 0 ? _options.DefaultOffset : query.Offset);
        EverythingNative.SetMax(query.MaxResults == 0 ? _options.DefaultMaxResults : query.MaxResults);
        EverythingNative.SetMatchPath(query.MatchPath);
        EverythingNative.SetMatchCase(query.MatchCase);
        EverythingNative.SetMatchWholeWord(query.MatchWholeWord);
        EverythingNative.SetRegex(query.Regex);

        if (query.Sort.HasValue)
        {
            EverythingNative.SetSort((uint)query.Sort.Value);
        }
    }

    private static IReadOnlyList<EverythingSearchResult> ReadResults(uint total, EverythingRequestFlags requestFlags)
    {
        var list = new List<EverythingSearchResult>((int)total);

        for (uint i = 0; i < total; i++)
        {
            var fileName = EverythingNative.GetResultFileName(i);
            var path = EverythingNative.GetResultPath(i);
            var fullPath = EverythingNative.GetResultFullPath(i);

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                fullPath = string.IsNullOrWhiteSpace(path)
                    ? fileName
                    : Path.Combine(path, fileName);
            }

            var isFolder = EverythingNative.IsFolderResult(i);

            list.Add(new EverythingSearchResult
            {
                Index = i,
                FileName = fileName,
                Path = path,
                FullPath = fullPath,
                Extension = requestFlags.HasFlag(EverythingRequestFlags.Extension) ? NullIfEmpty(EverythingNative.GetResultExtension(i)) : null,
                Size = requestFlags.HasFlag(EverythingRequestFlags.Size) && EverythingNative.TryGetResultSize(i, out var size) ? size : null,
                DateCreated = requestFlags.HasFlag(EverythingRequestFlags.DateCreated) && EverythingNative.TryGetResultDateCreated(i, out var dc) ? FileTimeConverter.FromNative(dc) : null,
                DateModified = requestFlags.HasFlag(EverythingRequestFlags.DateModified) && EverythingNative.TryGetResultDateModified(i, out var dm) ? FileTimeConverter.FromNative(dm) : null,
                DateAccessed = requestFlags.HasFlag(EverythingRequestFlags.DateAccessed) && EverythingNative.TryGetResultDateAccessed(i, out var da) ? FileTimeConverter.FromNative(da) : null,
                Attributes = requestFlags.HasFlag(EverythingRequestFlags.Attributes) ? EverythingNative.GetResultAttributes(i) : null,
                FileListFileName = requestFlags.HasFlag(EverythingRequestFlags.FileListFileName) ? NullIfEmpty(EverythingNative.GetResultFileListFileName(i)) : null,
                RunCount = requestFlags.HasFlag(EverythingRequestFlags.RunCount) ? EverythingNative.GetResultRunCount(i) : null,
                DateRun = requestFlags.HasFlag(EverythingRequestFlags.DateRun) && EverythingNative.TryGetResultDateRun(i, out var dr) ? FileTimeConverter.FromNative(dr) : null,
                DateRecentlyChanged = requestFlags.HasFlag(EverythingRequestFlags.DateRecentlyChanged) && EverythingNative.TryGetResultDateRecentlyChanged(i, out var rc) ? FileTimeConverter.FromNative(rc) : null,
                HighlightedFileName = requestFlags.HasFlag(EverythingRequestFlags.HighlightedFileName) ? NullIfEmpty(EverythingNative.GetResultHighlightedFileName(i)) : null,
                HighlightedPath = requestFlags.HasFlag(EverythingRequestFlags.HighlightedPath) ? NullIfEmpty(EverythingNative.GetResultHighlightedPath(i)) : null,
                HighlightedFullPath = requestFlags.HasFlag(EverythingRequestFlags.HighlightedFullPathAndFileName) ? NullIfEmpty(EverythingNative.GetResultHighlightedFullPath(i)) : null,
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
        var message = $"Everything native DLL was not found, is incompatible, or the current process architecture is unsupported. Expected native DLL: {EverythingNative.ExpectedDllName}";

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
