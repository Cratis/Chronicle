// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures seeding and observer settlement while keeping counters active.
/// </summary>
/// <param name="fixture">The cluster fixture.</param>
/// <param name="observer">The subscribed observer.</param>
/// <param name="counters">The benchmark counters.</param>
internal sealed class LiveInterleaveMeasurement(ClusterBenchmarkFixture fixture, CatchUpShapeObserver observer, CatchUpShapeCounters counters)
{
    /// <summary>
    /// Appends the dataset and measures both observers reaching the tail from seed start.
    /// </summary>
    /// <param name="settings">The benchmark settings.</param>
    /// <param name="dataset">The interleaved dataset.</param>
    /// <param name="marker">The handled marker sequence number.</param>
    /// <returns>The tail, elapsed times and pattern samples.</returns>
    /// <exception cref="InvalidOperationException">Thrown when seeding does not advance the tail.</exception>
    /// <exception cref="TimeoutException">Thrown when patterns do not settle within the configured wait.</exception>
    public async Task<(ulong Tail, double SeedElapsedMilliseconds, double ObserverElapsedMilliseconds, double PatternsElapsedMilliseconds, object[] Samples)> Measure(
        CatchUpShapeSettings settings, CatchUpShapeDataset dataset, ulong marker)
    {
        var timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        counters.Start();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await dataset.SeedBacklog(fixture).WaitAsync(timeout);
            var seedElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            var tail = (await fixture.EventStore1.EventLog.GetTailSequenceNumber().WaitAsync(timeout)).Value;
            if (tail <= marker) throw new InvalidOperationException("Seeding did not create a backlog beyond the marker.");
            await observer.WaitUntilCaughtUp(fixture, tail, timeout);
            var observerElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            var sampler = new CatchUpShapePatternSamples(counters.NamespaceDatabase, stopwatch, TimeSpan.FromSeconds(5));
            var patterns = await sampler.Read(timeout, TimeSpan.FromSeconds(settings.PatternsWaitSeconds), tail, settings.Events + 1L);
            if (!patterns.PatternsSettled || sampler.PatternsElapsedMilliseconds is null)
            {
                throw new TimeoutException($"The patterns observer did not settle at {tail} within {settings.PatternsWaitSeconds} seconds.");
            }

            return (tail, seedElapsedMilliseconds, observerElapsedMilliseconds, sampler.PatternsElapsedMilliseconds.Value, patterns.Samples);
        }
        finally
        {
            stopwatch.Stop();
            counters.Stop();
        }
    }
}
