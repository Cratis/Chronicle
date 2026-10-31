// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Measures bytes allocated per operation on the calling thread and reports them for before/after comparison.
/// </summary>
static class AllocationProbe
{
    const int Warmup = 50;

    public static async Task<(long BytesPerOperation, double MicrosecondsPerOperation)> Measure(string name, int iterations, Func<Task> operation)
    {
        for (var i = 0; i < Warmup; i++) await operation();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++) await operation();
        stopwatch.Stop();
        var bytes = (GC.GetAllocatedBytesForCurrentThread() - before) / iterations;
        var microseconds = stopwatch.Elapsed.TotalMilliseconds * 1000 / iterations;
        var line = $"ALLOCPROBE {name} bytes/op={bytes} us/op={microseconds:F1} n={iterations}";
        Console.WriteLine(line);
        var path = Environment.GetEnvironmentVariable("ALLOC_PROBE_OUT");
        if (!string.IsNullOrEmpty(path)) await File.AppendAllTextAsync(path, line + Environment.NewLine);
        return (bytes, microseconds);
    }
}
