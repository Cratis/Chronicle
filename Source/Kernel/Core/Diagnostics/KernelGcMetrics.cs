// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Exposes what the garbage collector holds, so live and committed memory can be told apart on a running kernel.
/// </summary>
/// <remarks>
/// <para>
/// A forced full collection, which is what a heap dump does, shows what is live; it says nothing about what the
/// collector keeps committed after dormancy has released most observers. These gauges report both, per generation,
/// as of the last collection, together with the pause total and the allocation total so a rate can be derived.
/// Reading them costs one <see cref="GC.GetGCMemoryInfo()"/> call per scrape and allocates nothing that outlives it.
/// </para>
/// <para>
/// The numbers are per process, which is per silo. The generation tag is one of gen0, gen1,
/// gen2, loh and poh.
/// </para>
/// </remarks>
public sealed class KernelGcMetrics : IHostedService
{
    static readonly string[] _generations = ["gen0", "gen1", "gen2", "loh", "poh"];

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelGcMetrics"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> to report to.</param>
    public KernelGcMetrics([FromKeyedServices(WellKnown.MeterName)] IMeter<KernelGcMetrics> meter)
    {
        var actual = meter.ActualMeter;
        actual?.CreateObservableGauge(
            WellKnownMetricNames.GcHeapGenerationSize,
            () => ObserveGenerations(info => info.SizeAfterBytes),
            WellKnownMetricNames.GcHeapGenerationSizeUnit,
            "Bytes held live by a heap generation after the last garbage collection");
        actual?.CreateObservableGauge(
            WellKnownMetricNames.GcHeapGenerationFragmentation,
            () => ObserveGenerations(info => info.FragmentationAfterBytes),
            WellKnownMetricNames.GcHeapGenerationFragmentationUnit,
            "Bytes of a heap generation lost to fragmentation after the last garbage collection");
        actual?.CreateObservableGauge(
            WellKnownMetricNames.GcHeapSize,
            () => GC.GetGCMemoryInfo().HeapSizeBytes,
            WellKnownMetricNames.GcHeapSizeUnit,
            "Size of the whole managed heap as of the last garbage collection");
        actual?.CreateObservableGauge(
            WellKnownMetricNames.GcHeapCommitted,
            () => GC.GetGCMemoryInfo().TotalCommittedBytes,
            WellKnownMetricNames.GcHeapCommittedUnit,
            "Memory the garbage collector has committed from the operating system, as of the last garbage collection");
        actual?.CreateObservableCounter(
            WellKnownMetricNames.GcPauseDuration,
            () => GC.GetTotalPauseDuration().TotalSeconds,
            WellKnownMetricNames.GcPauseDurationUnit,
            "Total time the process has spent paused by garbage collection");
        actual?.CreateObservableCounter(
            WellKnownMetricNames.GcAllocated,
            () => GC.GetTotalAllocatedBytes(),
            WellKnownMetricNames.GcAllocatedUnit,
            "Total bytes the process has allocated");
        actual?.CreateObservableCounter(
            WellKnownMetricNames.GcCollections,
            ObserveCollections,
            WellKnownMetricNames.GcCollectionsUnit,
            "Number of garbage collections that have happened, by generation");
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    static Measurement<long>[] ObserveGenerations(Func<GCGenerationInfo, long> select)
    {
        var info = GC.GetGCMemoryInfo().GenerationInfo;
        var measurements = new Measurement<long>[Math.Min(info.Length, _generations.Length)];
        for (var index = 0; index < measurements.Length; index++)
        {
            measurements[index] = new(select(info[index]), new KeyValuePair<string, object?>("generation", _generations[index]));
        }

        return measurements;
    }

    static Measurement<long>[] ObserveCollections()
    {
        var measurements = new Measurement<long>[3];
        for (var generation = 0; generation < measurements.Length; generation++)
        {
            measurements[generation] = new(GC.CollectionCount(generation), new KeyValuePair<string, object?>("generation", _generations[generation]));
        }

        return measurements;
    }
}
