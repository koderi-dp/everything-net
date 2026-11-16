using Everything.Net.Configuration;
using Everything.Net.Enums;
using Everything.Net.Models;
using Everything.Net.Services;
using Microsoft.Extensions.Options;

namespace Everything.Net.Benchmarks;

public enum FieldSet
{
    /// <summary>File name and path only (the library default).</summary>
    NameAndPath,

    /// <summary>Name, path, extension, size, dates and attributes.</summary>
    Metadata
}

public enum ResultOrder
{
    /// <summary>Sorted by name: results come from scattered folders.</summary>
    ByName,

    /// <summary>Sorted by path: neighbouring results share a folder.</summary>
    ByPath
}

/// <summary>
/// The queries every report runs, so numbers stay comparable between runs.
/// Override the search text with the EVERYTHING_BENCH_QUERY environment variable.
/// </summary>
public static class SearchScenario
{
    public static string SearchText { get; } =
        Environment.GetEnvironmentVariable("EVERYTHING_BENCH_QUERY") is { Length: > 0 } text ? text : "*.dll";

    public static EverythingSdkVersion Sdk { get; } =
        Enum.TryParse<EverythingSdkVersion>(Environment.GetEnvironmentVariable("EVERYTHING_BENCH_SDK"), ignoreCase: true, out var sdk)
            ? sdk
            : EverythingSdkVersion.Auto;

    public static EverythingClient CreateClient() =>
        new(Options.Create(new EverythingClientOptions { SdkVersion = Sdk }));

    public static EverythingQuery CreateQuery(uint maxResults, FieldSet fields, ResultOrder order) => new()
    {
        SearchText = SearchText,
        MaxResults = maxResults,
        Sort = order == ResultOrder.ByPath ? EverythingSort.PathAscending : EverythingSort.NameAscending,
        RequestFlags = fields == FieldSet.Metadata
            ? EverythingRequestFlags.FileName |
              EverythingRequestFlags.Path |
              EverythingRequestFlags.Extension |
              EverythingRequestFlags.Size |
              EverythingRequestFlags.DateCreated |
              EverythingRequestFlags.DateModified |
              EverythingRequestFlags.Attributes
            : EverythingRequestFlags.FileName | EverythingRequestFlags.Path
    };
}
