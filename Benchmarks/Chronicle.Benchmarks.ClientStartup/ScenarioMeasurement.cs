// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Measures complete scenario operations, including allocation and post-GC retention.
/// </summary>
internal static class ScenarioMeasurement
{
    const int Samples = 30;

    /// <summary>
    /// Records the first invocation, then samples a verified operation after three warmups.
    /// </summary>
    /// <param name="name">The operation name.</param>
    /// <param name="run">The operation, which must fail if delivery or materialization did not happen.</param>
    /// <returns>First-run, median and p95 timing, allocations, and retained bytes.</returns>
    internal static async Task<string> Measure(string name, Func<Task> run)
    {
        var first = await Sample(run);
        await run();
        await run();
        var times = new double[Samples];
        var allocations = new long[Samples];
        var before = RetainedBytes();
        for (var index = 0; index < Samples; index++)
        {
            var sample = await Sample(run);
            times[index] = sample.Milliseconds;
            allocations[index] = sample.AllocatedBytes;
        }
        var retained = RetainedBytes() - before;
        Array.Sort(times);
        Array.Sort(allocations);
        return $"{name}: first={first.Milliseconds:F2}ms/{first.AllocatedBytes}B median={times[Samples / 2]:F2}ms/{allocations[Samples / 2]}B p95={times[(int)Math.Ceiling(Samples * 0.95) - 1]:F2}ms retained={retained}B/{Samples}runs";
    }

    /// <summary>
    /// Forces a compacting full collection and reads the bytes that survived it (informational, not a gate).
    /// </summary>
    /// <returns>The live (promoted) managed bytes, which exclude free and fragmented heap space.</returns>
    internal static long RetainedBytes()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        return GC.GetGCMemoryInfo(GCKind.FullBlocking).PromotedBytes;
    }

    static async Task<(double Milliseconds, long AllocatedBytes)> Sample(Func<Task> run)
    {
        var before = GC.GetTotalAllocatedBytes(true);
        var watch = Stopwatch.StartNew();
        await run();
        return (watch.Elapsed.TotalMilliseconds, GC.GetTotalAllocatedBytes(true) - before);
    }
}
