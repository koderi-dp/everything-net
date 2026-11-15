using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Everything.Net.Benchmarks;

/// <summary>
/// Measures what BenchmarkDotNet cannot: memory still held after a search returns.
/// "Managed held" is the heap kept alive by the response; "Native held" is the growth in
/// process memory outside the managed heap that remains once the response has been dropped.
/// </summary>
public static class RetainedMemoryReport
{
    private const uint MaxResults = 100_000;

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Console.WriteLine();
        Console.WriteLine($"Retained memory after one search (query '{SearchScenario.SearchText}', up to {MaxResults:N0} results)");
        Console.WriteLine();
        Console.WriteLine("| Fields      | Order  | Results | Managed held | Per result | Per result, FullPath read | Native held after release |");
        Console.WriteLine("|-------------|--------|--------:|-------------:|-----------:|--------------------------:|--------------------------:|");

        foreach (var fields in Enum.GetValues<FieldSet>())
        {
            foreach (var order in Enum.GetValues<ResultOrder>())
            {
                Measure(fields, order);
            }
        }

        Console.WriteLine();
    }

    private static void Measure(FieldSet fields, ResultOrder order)
    {
        var client = SearchScenario.CreateClient();

        // A one-result query first: keeps JIT, DLL load and IPC setup out of the numbers and
        // leaves the native library holding (almost) nothing before the measured search.
        client.Search(SearchScenario.CreateQuery(1, fields, order));

        var managedBefore = GC.GetTotalMemory(forceFullCollection: true);
        var nativeBefore = NativeBytes();

        var (count, managedHeld, heldWithFullPaths) = SearchAndMeasureHeld(client, fields, order, managedBefore);

        // The response is unreachable now; whatever is still up outside the GC heap is native.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var nativeHeld = NativeBytes() - nativeBefore;

        Console.WriteLine(
            $"| {fields,-11} | {order,-6} | {count,7:N0} | {Mb(managedHeld),12} | {PerResult(managedHeld, count),10} | {PerResult(heldWithFullPaths, count),25} | {Mb(nativeHeld),25} |");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (int Count, long ManagedHeld, long HeldWithFullPaths) SearchAndMeasureHeld(
        Services.EverythingClient client, FieldSet fields, ResultOrder order, long managedBefore)
    {
        var response = client.Search(SearchScenario.CreateQuery(MaxResults, fields, order));
        var managedHeld = GC.GetTotalMemory(forceFullCollection: true) - managedBefore;

        // Callers that read FullPath on every result pay for those strings too.
        long characters = 0;
        foreach (var result in response.Results)
        {
            characters += result.FullPath.Length;
        }

        var heldWithFullPaths = GC.GetTotalMemory(forceFullCollection: true) - managedBefore;
        GC.KeepAlive(response);
        GC.KeepAlive(characters);
        return (response.Results.Count, managedHeld, heldWithFullPaths);
    }

    /// <summary>Process private memory minus everything the GC has committed.</summary>
    private static long NativeBytes()
    {
        GC.Collect();
        using var process = Process.GetCurrentProcess();
        return process.PrivateMemorySize64 - GC.GetGCMemoryInfo().TotalCommittedBytes;
    }

    private static string PerResult(long bytes, int count) => $"{(count == 0 ? 0 : bytes / count):N0} B";

    private static string Mb(long bytes) => $"{bytes / (1024.0 * 1024.0):N1} MB";
}
