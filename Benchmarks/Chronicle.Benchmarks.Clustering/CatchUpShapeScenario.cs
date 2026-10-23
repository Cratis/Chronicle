// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Runs the interleaved catch-up scenario.
/// </summary>
internal static class CatchUpShapeScenario
{
    /// <summary>
    /// Runs the scenario and prints the result.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the marker or backlog could not be established.</exception>
    public static async Task Run()
    {
        var settings = CatchUpShapeSettings.FromEnvironment();
        var timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        var counters = new CatchUpShapeCounters();
        var dataset = new CatchUpShapeDataset(settings);
        await using var fixture = new ClusterBenchmarkFixture(ClusterTopology.SingleSilo, counters.Configure);
        await fixture.Start().WaitAsync(timeout);
        var observer = await CatchUpShapeObserver.Register(fixture, counters, settings).WaitAsync(timeout);
        if (!(await fixture.EventStore1.EventLog.Append(CatchUpShapeDataset.Source(0), new CatchUpShapeValueSet(0)).WaitAsync(timeout)).IsSuccess)
        {
            throw new InvalidOperationException("The catch-up marker was not acknowledged successfully.");
        }
        var marker = (await fixture.EventStore1.EventLog.GetTailSequenceNumber().WaitAsync(timeout)).Value;
        await observer.WaitUntilCaughtUp(fixture, marker, timeout);
        await observer.Disconnect().WaitAsync(timeout);
        await dataset.SeedBacklog(fixture).WaitAsync(timeout);
        var tail = (await fixture.EventStore1.EventLog.GetTailSequenceNumber().WaitAsync(timeout)).Value;
        if (tail <= marker) throw new InvalidOperationException("Seeding did not create a backlog beyond the marker.");
        await observer.VerifyBehind(marker).WaitAsync(timeout);

        observer.PrintSubscription(settings.ObserverType);
        var allocationsBefore = GC.GetTotalAllocatedBytes(true);
        var collectionsBefore = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
        counters.Start();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await observer.Reconnect(settings.ObserverType).WaitAsync(timeout);
            await observer.WaitUntilCaughtUp(fixture, tail, timeout);
        }
        finally
        {
            stopwatch.Stop();
            counters.Stop();
        }

        var allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocationsBefore;
        var gcCollections = new[] { GC.CollectionCount(0) - collectionsBefore[0], GC.CollectionCount(1) - collectionsBefore[1], GC.CollectionCount(2) - collectionsBefore[2] };
        counters.VerifyJobShape();
        var fingerprint = await dataset.Verify(fixture).WaitAsync(timeout);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Scenario = "interleaved-catch-up",
            settings.ObserverType,
            JobType = "CatchUpObserver",
            settings.Sources,
            settings.Events,
            settings.BatchSize,
            settings.TimeoutSeconds,
            MarkerSequenceNumber = marker,
            TailSequenceNumber = tail,
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            EventsPerSecond = settings.Events / stopwatch.Elapsed.TotalSeconds,
            AllocatedBytes = allocatedBytes,
            Gen0Collections = gcCollections[0],
            Gen1Collections = gcCollections[1],
            Gen2Collections = gcCollections[2],
            SnapshotFingerprint = fingerprint,
            Counters = counters.Snapshot(settings.Events),
            MongoBoundary = "Chronicle IMongoDBClientManager clients; excludes Orleans persistence/membership and Arc clients; includes readiness probes and background work",
            GrainBoundary = "All incoming and outgoing silo grain calls, including readiness probes and background work; excludes calls bypassing Orleans dispatch"
        }));
    }
}
