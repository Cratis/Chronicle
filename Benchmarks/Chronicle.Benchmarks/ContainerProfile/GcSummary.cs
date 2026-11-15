// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>GC counts, pause percentiles and effective heap counts from a separate trace.</summary>
internal sealed record GcSummary
{
    /// <summary>Gets completed collections.</summary>
    public required int GcCount { get; init; }

    /// <summary>Gets Gen0 collections.</summary>
    public required int Gen0 { get; init; }

    /// <summary>Gets Gen1 collections.</summary>
    public required int Gen1 { get; init; }

    /// <summary>Gets Gen2 collections, which also collect the LOH.</summary>
    public required int Gen2 { get; init; }

    /// <summary>Gets the minimum observed GC heap count.</summary>
    public int? HeapCountMin { get; init; }

    /// <summary>Gets the maximum observed GC heap count.</summary>
    public int? HeapCountMax { get; init; }

    /// <summary>Gets the median pause, or null.</summary>
    public double? GcPauseP50Ms { get; init; }

    /// <summary>Gets the 95th percentile pause, or null.</summary>
    public double? GcPauseP95Ms { get; init; }

    /// <summary>Gets the 99th percentile pause, or null.</summary>
    public double? GcPauseP99Ms { get; init; }

    /// <summary>Gets the longest pause, or null.</summary>
    public double? GcPauseMaxMs { get; init; }

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
        return new GcSummary
        {
            GcCount = counts.Sum(),
            Gen0 = counts[0],
            Gen1 = counts[1],
            Gen2 = counts[2],
            HeapCountMin = heaps.Count > 0 ? heaps.Min() : null,
            HeapCountMax = heaps.Count > 0 ? heaps.Max() : null,
            GcPauseP50Ms = Percentiles.NearestRank(pauses, 0.5),
            GcPauseP95Ms = Percentiles.NearestRank(pauses, 0.95),
            GcPauseP99Ms = Percentiles.NearestRank(pauses, 0.99),
            GcPauseMaxMs = pauses.Count > 0 ? pauses.Max() : null
        };
    }
}
