using BenchmarkDotNet.Running;
using Everything.Net.Benchmarks;

// Usage (Everything must be running):
//   dotnet run -c Release --project src/Everything.Net.Benchmarks              -> both reports
//   dotnet run -c Release --project src/Everything.Net.Benchmarks -- alloc     -> allocations per search (BenchmarkDotNet)
//   dotnet run -c Release --project src/Everything.Net.Benchmarks -- retained  -> memory held after a search
var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

if (!SearchScenario.CreateClient().IsAvailable())
{
    Console.Error.WriteLine("Everything is not running (or its database is not loaded). Start it and retry.");
    return 1;
}

if (mode is "all" or "retained")
{
    RetainedMemoryReport.Run();
}

if (mode is "all" or "alloc")
{
    BenchmarkRunner.Run<SearchAllocationBenchmarks>(args: args.Skip(1).ToArray());
}

return 0;
