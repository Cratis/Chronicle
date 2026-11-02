// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>GC counts, pause percentiles and effective heap counts from a separate trace.</summary>
/// <param name="GcCount">Completed collections.</param>
/// <param name="Gen0">Gen0 collections.</param>
/// <param name="Gen1">Gen1 collections.</param>
/// <param name="Gen2">Gen2 collections, which also collect the LOH.</param>
/// <param name="HeapCountMin">The minimum observed GC heap count.</param>
/// <param name="HeapCountMax">The maximum observed GC heap count.</param>
/// <param name="GcPauseP50Ms">The median pause, or null.</param>
/// <param name="GcPauseP95Ms">The 95th percentile pause, or null.</param>
/// <param name="GcPauseP99Ms">The 99th percentile pause, or null.</param>
/// <param name="GcPauseMaxMs">The longest pause, or null.</param>
internal sealed record GcSummary(int GcCount, int Gen0, int Gen1, int Gen2, int? HeapCountMin, int? HeapCountMax, double? GcPauseP50Ms, double? GcPauseP95Ms, double? GcPauseP99Ms, double? GcPauseMaxMs)
{
    /// <summary>Parses a gracefully flushed runtime trace, never an OOM-truncated file.</summary>
    /// <param name="path">The flushed nettrace file.</param>
    /// <returns>The GC summary, or null when unavailable.</returns>
    public static GcSummary? Read(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        var counts = new int[3];
        var pauses = new List<double>();
        var heaps = new List<int>();
        using var source = new EventPipeEventSource(path);
        source.NeedLoadedDotNetRuntimes();
        source.AddCallbackOnProcessStart(process => process.AddCallbackOnDotNetRuntimeLoad(runtime =>
            runtime.GCEnd += (_, gc) =>
            {
                counts[Math.Clamp(gc.Generation, 0, 2)]++;
                pauses.Add(gc.PauseDurationMSec);
                if (gc.HeapCount > 0)
                {
                    heaps.Add(gc.HeapCount);
                }
            }));
        source.Process();
        return new GcSummary(
            counts.Sum(),
            counts[0],
            counts[1],
            counts[2],
            heaps.Count > 0 ? heaps.Min() : null,
            heaps.Count > 0 ? heaps.Max() : null,
            Percentiles.NearestRank(pauses, 0.5),
            Percentiles.NearestRank(pauses, 0.95),
            Percentiles.NearestRank(pauses, 0.99),
            pauses.Count > 0 ? pauses.Max() : null);
    }
}
