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
        var bulks = counts.SinkBulkOperations;

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
            MongoBoundary = "Chronicle IMongoDBClientManager clients; excludes Orleans persistence/membership and Arc clients; includes readiness probes and background work",
            counts.GrainCalls,
            GrainCallsPerEvent = (double)counts.GrainCalls / settings.Events,
            GrainBoundary = "All incoming silo grain calls, including readiness probes and background work; excludes calls bypassing Orleans dispatch",
            SinkBulks = bulks.Count,
            SinkBulkOpsMean = bulks.Count == 0 ? 0 : bulks.Average(),
            SinkBulkOpsMax = bulks.Count == 0 ? 0 : bulks.Max(),
            SinkBulksAtOrAbove1000 = bulks.Count(b => b >= 1000),
            SinkBulkOpsHistogram = new Dictionary<string, int>
            {
                ["1"] = bulks.Count(b => b == 1),
                ["2-99"] = bulks.Count(b => b is >= 2 and < 100),
                ["100-499"] = bulks.Count(b => b is >= 100 and < 500),
                ["500-999"] = bulks.Count(b => b is >= 500 and < 1000),
                ["1000+"] = bulks.Count(b => b >= 1000)
            },
            SinkBulkBoundary = "Insert/update/delete commands on collections whose name contains the read-model type name, grouped by driver OperationId (one ordered bulk write); operations are the summed documents/updates/deletes array lengths",
            counts.FlushForCalls,
            FlushForCallsPer1000Events = counts.FlushForCalls * 1000.0 / settings.Events,
            FlushForBoundary = "Incoming IObserverService.FlushFor grain-service calls (one per FlushAllFor on a single silo); 0 means grain-service calls bypassed the filter",
            counts.EventCursorPages,
            EventCursorPagesBoundary = "getMore commands on the event-log collection plus one initial find; a lower bound on event cursor pages across all readers in the window",
            Before = new { before.Count, before.Hash },
            After = new { after.Count, after.Hash }
        }));
    }
}
