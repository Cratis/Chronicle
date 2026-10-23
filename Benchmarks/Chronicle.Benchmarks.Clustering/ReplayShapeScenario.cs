// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Replays a deterministic interleaved child hierarchy and reports one measurement.
/// </summary>
internal static class ReplayShapeScenario
{
    /// <summary>
    /// Runs the bounded scenario and writes its JSON measurement.
    /// </summary>
    /// <returns>A task representing scenario execution.</returns>
    /// <exception cref="InvalidOperationException">The replay does not preserve the verified contents.</exception>
    internal static async Task Run()
    {
        var settings = ReplayShapeSettings.Read();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        var counters = new ReplayShapeCounters();
        await using var fixture = new ClusterBenchmarkFixture(ClusterTopology.SingleSilo, counters.Configure);
        await Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} Starting single-silo fixture.");
        await fixture.Start().WaitAsync(deadline.Token);
        var dataset = new ReplayShapeDataset(fixture, settings);
        await fixture.Readiness.WaitForSubscribedObserver(dataset.Handler.Id, TimeSpan.FromSeconds(settings.TimeoutSeconds)).WaitAsync(deadline.Token);
        await dataset.Seed(deadline.Token);
        await Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} Seed caught up; verifying contents.");
        var before = await dataset.Snapshot(deadline.Token);
        var projection = fixture.EventStore1.Projections.GetProjectionIdForModel<ReplayShapeReadModel>();

        await Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} Requesting full replay.");
        counters.Start();
        var clock = Stopwatch.StartNew();
        var job = await fixture.EventStore1.Projections.Replay(projection).WaitAsync(deadline.Token);
        await fixture.EventStore1.Jobs.WaitTillJobCompletesOrIsDeleted(job, TimeSpan.FromSeconds(settings.TimeoutSeconds)).WaitAsync(deadline.Token);
        await dataset.CaughtUp(deadline.Token);
        clock.Stop();
        var counts = counters.Stop();

        await Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} Replay complete; verifying contents.");
        var after = await dataset.Snapshot(deadline.Token);
        if (before != after) throw new InvalidOperationException("Replay changed the read-model count or content hash.");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Scenario = "interleaved-nested-replay",
            Workload = settings,
            ReplayShapeDataset.RandomSeed,
            EventCount = settings.Events,
            dataset.RunLengthHistogram,
            RunLengthHistogramBoundary = "Exact same-source run lengths across all workload events, including creation and removal; values count runs, not events",
            ReplaySeconds = clock.Elapsed.TotalSeconds,
            EventsPerSecond = settings.Events / clock.Elapsed.TotalSeconds,
            DocumentsWritten = after.Count,
            counts.MongoCommands,
            MongoCommandsPerEvent = (double)counts.MongoCommands / settings.Events,
            counts.MongoFindCommandsByTag,
            MongoFindCommandsPerEventByTag = counts.MongoFindCommandsByTag.ToDictionary(_ => _.Key, _ => (double)_.Value / settings.Events),
            MongoBoundary = "Chronicle IMongoDBClientManager clients; excludes Orleans persistence/membership and Arc clients; includes readiness probes and background work",
            counts.GrainCalls,
            GrainCallsPerEvent = (double)counts.GrainCalls / settings.Events,
            GrainBoundary = "All incoming silo grain calls, including readiness probes and background work; excludes calls bypassing Orleans dispatch",
            Before = new { before.Count, before.Hash },
            After = new { after.Count, after.Hash }
        }));
    }
}
