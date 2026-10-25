// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures a live observer keeping up with interleaved appends.
/// </summary>
internal static class LiveInterleaveScenario
{
    /// <summary>
    /// Runs the scenario without disconnecting the observer and prints the result.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when appends or snapshot verification fail.</exception>
    /// <exception cref="TimeoutException">Thrown when an observer does not reach the tail in time.</exception>
    public static async Task Run()
    {
        var settings = CatchUpShapeSettings.FromLiveEnvironment();
        var timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        var counters = new CatchUpShapeCounters();
        var dataset = new CatchUpShapeDataset(settings);
        using var subscriptionControl = new CatchUpShapeSubscriptionControl();
        await using var fixture = CatchUpShapeScenario.CreateFixture(counters, subscriptionControl);
        await fixture.Start().WaitAsync(timeout);
        var observer = await CatchUpShapeObserver.Register(fixture, counters, settings, subscriptionControl).WaitAsync(timeout);
        var marker = await CatchUpShapeScenario.AppendMarker(fixture, timeout);
        await observer.WaitUntilCaughtUp(fixture, marker, timeout);

        var result = await new LiveInterleaveMeasurement(fixture, observer, counters).Measure(settings, dataset, marker);
        var fingerprint = await dataset.Verify(fixture).WaitAsync(timeout);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Scenario = "live-interleave",
            settings.ObserverType,
            settings.Sources,
            settings.Events,
            settings.BatchSize,
            settings.AppendIntervalMs,
            MarkerSequenceNumber = marker,
            TailSequenceNumber = result.Tail,
            result.SeedElapsedMilliseconds,
            result.ObserverElapsedMilliseconds,
            ObserverEventsPerSecond = settings.Events / (result.ObserverElapsedMilliseconds / 1000),
            PatternsSettled = true,
            result.PatternsElapsedMilliseconds,
            PatternsEventsPerSecond = settings.Events / (result.PatternsElapsedMilliseconds / 1000),
            counters.CatchUpObserverStarts,
            PatternsObserverSamples = result.Samples,
            SnapshotFingerprint = fingerprint,
            Counters = counters.Snapshot(settings.Events)
        }));
    }
}
