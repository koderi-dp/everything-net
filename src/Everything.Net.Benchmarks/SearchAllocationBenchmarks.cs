using BenchmarkDotNet.Attributes;
using Everything.Net.Models;
using Everything.Net.Services;

namespace Everything.Net.Benchmarks;

/// <summary>
/// Managed bytes allocated by one <c>Search</c> call against the running Everything instance.
/// The "Allocated" column is the number to compare between runs.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class SearchAllocationBenchmarks
{
    private EverythingClient _client = null!;
    private EverythingQuery _query = null!;

    [Params(100u, 100_000u)]
    public uint MaxResults { get; set; }

    [Params(FieldSet.NameAndPath, FieldSet.Metadata)]
    public FieldSet Fields { get; set; }

    [Params(ResultOrder.ByName, ResultOrder.ByPath)]
    public ResultOrder Order { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _client = SearchScenario.CreateClient();
        _query = SearchScenario.CreateQuery(MaxResults, Fields, Order);

        var response = _client.Search(_query);
        if (!response.Success || response.Results.Count == 0)
        {
            throw new InvalidOperationException(
                $"Query '{SearchScenario.SearchText}' returned no results; is Everything running?");
        }
    }

    [Benchmark]
    public EverythingQueryResponse Search() => _client.Search(_query);
}
