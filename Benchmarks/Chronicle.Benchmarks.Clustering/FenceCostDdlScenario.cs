// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Projections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Counts the schema operations (create, drop, rename, index) MongoDB serves while a projection takes steady-state appends and
/// while it is replayed, from the server's command metrics around each phase. Flag-off kernel; one JSON line per phase.
/// </summary>
internal static class FenceCostDdlScenario
{
    static readonly string[] _commands = ["create", "drop", "dropDatabase", "renameCollection", "createIndexes", "dropIndexes", "collMod"];

    /// <summary>
    /// Runs the scenario.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal static async Task Run()
    {
        var eventCount = int.Parse(Environment.GetEnvironmentVariable("FENCE_DDL_EVENTS") ?? "5000");
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var fixture = new ClusterBenchmarkFixture(ClusterTopology.SingleSilo);
        await fixture.Start();
        try
        {
            var admin = fixture.ReadModelDatabase.Client.GetDatabase("admin");
            var dataset = new LiveRebuildDataset(fixture, eventCount);

            var before = await Read(admin);
            await dataset.Seed().WaitAsync(deadline.Token);
            var seeded = await Read(admin);
            Emit("steady-state-appends", eventCount + 2, seeded, before);

            var original = await dataset.ServedGeneration();
            var tail = await fixture.EventStore1.EventLog.GetTailSequenceNumber();
            await dataset.Replay().WaitAsync(deadline.Token);
            var clock = Stopwatch.StartNew();
            while (Equals(await dataset.ServedGeneration(), original))
            {
                deadline.Token.ThrowIfCancellationRequested();
                await Task.Delay(100, deadline.Token);
            }

            await dataset.Handler.WaitTillActive().WaitAsync(deadline.Token);
            await dataset.Handler.WaitTillReachesEventSequenceNumber(tail).WaitAsync(deadline.Token);
            await dataset.Verify().WaitAsync(deadline.Token);
            Emit("replay", eventCount + 2, await Read(admin), seeded, clock.Elapsed.TotalSeconds);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    static async Task<Dictionary<string, long>> Read(IMongoDatabase admin)
    {
        var status = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("serverStatus", 1));
        var commands = status["metrics"]["commands"].AsBsonDocument;
        return _commands.ToDictionary(name => name, name => commands.TryGetValue(name, out var metric) ? metric["total"].ToInt64() : 0, StringComparer.Ordinal);
    }

    static void Emit(string phase, int events, Dictionary<string, long> after, Dictionary<string, long> before, double? seconds = null)
    {
        var counts = _commands.ToDictionary(name => name, name => after[name] - before[name], StringComparer.Ordinal);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Scenario = "sink-ddl-frequency",
            Phase = phase,
            Events = events,
            Seconds = seconds,
            Counts = counts,
            PerThousandEvents = counts.ToDictionary(pair => pair.Key, pair => pair.Value * 1000d / events, StringComparer.Ordinal)
        }));
    }
}
