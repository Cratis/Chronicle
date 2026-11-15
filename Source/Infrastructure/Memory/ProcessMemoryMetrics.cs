// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Memory;

/// <summary>
/// Reports how the process uses memory.
/// </summary>
/// <remarks>
/// The numbers that tell memory use from memory pressure: how large the managed heap is against what is committed
/// and what the process may use, how fragmented it is, and how fast it is being allocated and collected. They are
/// read from the runtime when metrics are collected, so reporting them costs nothing in between.
/// </remarks>
public static class ProcessMemoryMetrics
{
    /// <summary>
    /// Register the process memory instruments on a meter.
    /// </summary>
    /// <param name="meter">The <see cref="Meter"/> to register on.</param>
    public static void Register(Meter meter)
    {
        meter.CreateObservableGauge("cratis.chronicle.memory.heap_size", () => GC.GetGCMemoryInfo().HeapSizeBytes, "By", "Size of the managed heap after the last collection");
        meter.CreateObservableGauge("cratis.chronicle.memory.committed", () => GC.GetGCMemoryInfo().TotalCommittedBytes, "By", "Memory the garbage collector holds from the system");
        meter.CreateObservableGauge("cratis.chronicle.memory.available", () => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, "By", "Memory available to the process, as limited by the machine, container or heap hard limit");
        meter.CreateObservableGauge("cratis.chronicle.memory.load", () => GC.GetGCMemoryInfo().MemoryLoadBytes, "By", "Memory in use on the machine or in the container as the garbage collector sees it");
        meter.CreateObservableGauge("cratis.chronicle.memory.fragmentation", () => GC.GetGCMemoryInfo().FragmentedBytes, "By", "Fragmented memory in the managed heap after the last collection");
        meter.CreateObservableGauge("cratis.chronicle.memory.pause_time_ratio", () => GC.GetGCMemoryInfo().PauseTimePercentage, "%", "Share of time spent paused for garbage collection");
        meter.CreateObservableGauge("cratis.chronicle.memory.working_set", () => Environment.WorkingSet, "By", "Physical memory of the process, managed and not");
        meter.CreateObservableCounter("cratis.chronicle.memory.allocated", () => GC.GetTotalAllocatedBytes(), "By", "Total bytes allocated on the managed heap");
        meter.CreateObservableCounter("cratis.chronicle.memory.gen2_collections", () => GC.CollectionCount(2), "{collection}", "Full garbage collections");
    }
}
