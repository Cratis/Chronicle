// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

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
        using var subscriptionControl = new CatchUpShapeSubscriptionControl();
        await using var fixture = CreateFixture(counters, subscriptionControl);
        await fixture.Start().WaitAsync(timeout);
        var observer = await CatchUpShapeObserver.Register(fixture, counters, settings, subscriptionControl).WaitAsync(timeout);
        var marker = await AppendMarker(fixture, timeout);
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
        var patterns = await new CatchUpShapePatternSamples(counters.NamespaceDatabase).Read(
            timeout, TimeSpan.FromSeconds(settings.PatternsWaitSeconds), tail, settings.Events + 1L);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Scenario = "interleaved-catch-up",
            settings.ObserverType,
            JobType = "CatchUpObserver",
            settings.Sources,
            settings.Events,
            settings.BatchSize,
            settings.TimeoutSeconds,
            settings.PatternsWaitSeconds,
            MarkerSequenceNumber = marker,
            TailSequenceNumber = tail,
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            EventsPerSecond = settings.Events / stopwatch.Elapsed.TotalSeconds,
            AllocatedBytes = allocatedBytes,
            Gen0Collections = gcCollections[0],
            Gen1Collections = gcCollections[1],
            Gen2Collections = gcCollections[2],
            SnapshotFingerprint = fingerprint,
            PatternsExpectedHandledEventCount = (long)settings.Events + 1,
            PatternsObserverSamples = patterns.Samples,
            patterns.PatternsSettled,
            patterns.PatternsHandledShortfall,
            Counters = counters.Snapshot(settings.Events),
            MongoBoundary = "Chronicle IMongoDBClientManager clients; excludes Orleans persistence/membership and Arc clients; includes readiness probes and background work",
            GrainBoundary = "All incoming and outgoing silo grain calls, including readiness probes and background work; excludes calls bypassing Orleans dispatch"
        }));
    }

    /// <summary>
    /// Appends the marker used to establish a live observer.
    /// </summary>
    /// <param name="fixture">The cluster fixture.</param>
    /// <param name="timeout">The append and tail-read timeout.</param>
    /// <returns>The marker sequence number.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the marker is not acknowledged successfully.</exception>
    public static async Task<ulong> AppendMarker(ClusterBenchmarkFixture fixture, TimeSpan timeout)
    {
        if (!(await fixture.EventStore1.EventLog.Append(CatchUpShapeDataset.Source(0), new CatchUpShapeValueSet(0)).WaitAsync(timeout)).IsSuccess)
        {
            throw new InvalidOperationException("The catch-up marker was not acknowledged successfully.");
        }
        return (await fixture.EventStore1.EventLog.GetTailSequenceNumber().WaitAsync(timeout)).Value;
    }

    /// <summary>
    /// Creates the single-silo fixture with counting and subscription filters.
    /// </summary>
    /// <param name="counters">The benchmark counters.</param>
    /// <param name="subscriptionControl">The subscription control.</param>
    /// <returns>The cluster fixture.</returns>
    public static ClusterBenchmarkFixture CreateFixture(CatchUpShapeCounters counters, CatchUpShapeSubscriptionControl subscriptionControl) =>
        new(ClusterTopology.SingleSilo, services =>
        {
            counters.Configure(services);
            services.AddSingleton<IIncomingGrainCallFilter>(subscriptionControl);
        });
}
