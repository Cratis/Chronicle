// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks;

internal static partial class Probe
{
    static async Task MeasurePull(ChronicleBenchmarkFixture fixture, ProbeMongo mongo, ProbeRun run, CancellationToken cancellation)
    {
        var parts = run.Scenario.Split(':');
        var catchup = parts[0] == "catchup";
        if (!catchup && parts[0] != "live") throw new ArgumentException("Pull supports live or catchup.");
        var n = int.Parse(parts[2]);
        var keys = int.Parse(parts[3]);
        var concurrency = int.Parse(parts[4]);
        run.Requested = n + (parts.Contains("overlap1000") ? 1000 : 0);
        var options = ProbePullOptions.FromEnvironment();
        run.PullOptions = options;
        run.Phase = "seeder-connection";
        using var seeder = new ChronicleClientHelper(fixture, run.EventStore, new ObserverScopedClientArtifactsProvider());
        await seeder.WaitForConnection().WaitAsync(cancellation);
        CheckObservers(await mongo.Observers().WaitAsync(cancellation), 0, false);
        if (catchup)
        {
            run.Phase = "seeding";
            var timer = Stopwatch.StartNew();
            await Append(seeder.EventLog, run, n, keys, concurrency, parts.Contains("batch"), cancellation);
            run.AppendMs = timer.Elapsed.TotalMilliseconds;
            run.Phase = "system-drain";
            await ProbeSystemDrain.Run(mongo, run, cancellation);
        }
        ProbePullMongoSource source = null!;
        ProbePullProgress progress = null!;
        ProbePullCoordinator? coordinator = null;
        var commands = new ProbePullCommands();
        async Task<ProbePullCoordinator> Start()
        {
            var settings = MongoClientSettings.FromConnectionString($"mongodb://localhost:{Environment.GetEnvironmentVariable("BENCH_MONGO_PORT") ?? "27018"}/?directConnection=true&serverSelectionTimeoutMS=5000");
            settings.ClusterConfigurator = builder => builder.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(commands.Record);
            var client = new MongoClient(settings);
            var database = client.GetDatabase($"{run.EventStore}+es+Default");
            source = new(database);
            progress = new(database, options.Stats);
            await progress.AtTail(0, cancellation); // First progress read is included in catch-up registration.
            return new(source, progress, options, evt =>
            {
                var payload = evt.Payload!;
                if (payload.Value >= 0) run.Delivery.Record(new(evt.Key, evt.Sequence), payload.Value, payload);
                return Task.CompletedTask;
            }, cancellation);
        }
        JsonElement? cpuBefore = null;
        long readsAtStart = 0;
        try
        {
            if (!catchup)
            {
                run.Phase = "live-warmup";
                coordinator = await Start();
                var warm = await seeder.EventLog.Append(EventSourceId.New(), new BenchmarkEvent("warm", -1, DateTimeOffset.UtcNow),
                    concurrencyScope: ConcurrencyScope.None).WaitAsync(cancellation);
                if (!warm.IsSuccess || warm.SequenceNumber.Value != 0) throw new InvalidOperationException("Pull live warmup must be event zero.");
                coordinator.Wake();
                var timer = Stopwatch.StartNew();
                while (true)
                {
                    coordinator.CheckHealth();
                    if (await progress.AtTail(0, cancellation) && StateAtTail(await mongo.State().WaitAsync(cancellation), 0, "$system.patterns")) break;
                    if (timer.Elapsed.TotalSeconds > 30) throw new TimeoutException("Pull warmup did not persist event zero.");
                    await Task.Delay(100, cancellation);
                }
            }
            run.BeforeObservers = (await mongo.Observers().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
            CheckObservers(run.BeforeObservers.Select(BsonDocument.Parse).ToArray(), 0, false);
            run.InitialObserverState = (await mongo.State().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
            run.Phase = "profile-setup";
            if (run.ProfileEnabled) run.ProfileStart = await mongo.StartProfile(run.ProbeObserverId).WaitAsync(cancellation);
            cpuBefore = await mongo.CpuSnapshot().WaitAsync(cancellation);
            readsAtStart = coordinator?.Reads ?? 0; // Exclude live warmup; catch-up starts the reader inside this window.
            run.Started = Stopwatch.GetTimestamp();
            run.CpuStart = Process.GetCurrentProcess().TotalProcessorTime;
            run.Clock.Start();
            commands.Start();
            run.Phase = "probe-registration";
            coordinator ??= await Start();
            run.RegisteredMs = run.Clock.Elapsed.TotalMilliseconds;
            if (run.ProfileEnabled) await mongo.MarkRegistered().WaitAsync(cancellation);
            run.Wake = coordinator.Wake;
            run.Observers = (await mongo.Observers().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
            CheckObservers(run.Observers.Select(BsonDocument.Parse).ToArray(), 0, false);
            run.ObserverListPassed = true;
            run.Phase = "measurement";
            if (!catchup || parts.Contains("overlap1000"))
            {
                await Append(seeder.EventLog, run, catchup ? 1000 : n, keys, catchup ? 16 : concurrency, parts.Contains("batch"), cancellation);
                run.AppendMs = run.Clock.Elapsed.TotalMilliseconds;
            }
            if (run.Expected != run.Requested) throw new InvalidOperationException("Pull acknowledged membership count differs from requested.");
            var tail = (await mongo.SequenceTail("event-log").WaitAsync(cancellation))!.Value;
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                coordinator.CheckHealth();
                run.Snapshot();
                var states = await mongo.State().WaitAsync(cancellation);
                run.PersistedObserverState = states.Select(d => d.ToJson()).ToArray();
                if (StateAtTail(states, tail, "$system.patterns")) run.SystemDurableMs ??= run.Clock.Elapsed.TotalMilliseconds;
                else if (Environment.GetEnvironmentVariable("PROBE_DRAIN_ALLOW_INFLIGHT") == "1" && StateAtTail(states, tail, "$system.patterns", allowInFlight: true))
                {
                    run.SystemDurableMs ??= run.Clock.Elapsed.TotalMilliseconds;
                    run.SystemDrainStatus = "COMPLETE_WITH_INFLIGHT";
                }
                if (await progress.AtTail(tail, cancellation) && !run.DurableMs.HasValue)
                {
                    run.DurableMs = run.Clock.Elapsed.TotalMilliseconds;
                    run.PullReadsAtDurable = coordinator.Reads - readsAtStart;
                }
                // The planted loss must finish at the saved tail instead of waiting for a missing effect forever.
                if (run.DurableMs.HasValue && run.SystemDurableMs.HasValue)
                {
                    await Task.Delay(2000, cancellation);
                    coordinator.CheckHealth();
                    await progress.AtTail(tail, cancellation);
                    var final = BsonDocument.Parse(progress.FinalDocument!);
                    run.PullTailExact = final["w"].ToDecimal() == tail && final["r"].ToDecimal() == tail;
                    if (run.PullTailExact != true) throw new ProbePullCorrectnessFailure("Final pull w/r does not equal event-log tail.");
                    await progress.CaptureCounts(cancellation);
                    var expectedCounts = run.Appended.GroupBy(p => p.Key.Key).ToDictionary(g => g.Key, g => (long)g.Count());
                    if (options.Stats == "equivalent")
                    {
                        var counts = progress.FinalCounts.Select(BsonDocument.Parse).ToDictionary(d => d["_id"]["partition"].AsString,
                            d => d["counts"][ProbePullMongoSource.EventType].ToInt64());
                        if (expectedCounts.Any(p => counts.GetValueOrDefault(p.Key) != p.Value) || counts.Values.Sum() != run.Expected + (catchup ? 0 : 1))
                            throw new ProbePullCorrectnessFailure("Pull per-key statistics do not match acknowledged membership plus warmup.");
                    }
                    if (final["handled"].ToInt64() != run.Expected + (catchup ? 0 : 1)) throw new ProbePullCorrectnessFailure("Pull checkpoint handled count mismatch.");
                    run.Status = run.ProfileEnabled ? "COMPLETE" : "COMPLETE_TIMING_ONLY";
                    return;
                }
                await Task.Delay(100, cancellation);
            }
        }
        catch (ProbePullCorrectnessFailure error)
        {
            run.PullCorrectnessFailure = error.ToString();
            throw;
        }
        finally
        {
            run.Wake = null;
            commands.End();
            run.PullWireCommands = JsonSerializer.SerializeToElement(commands.Snapshot());
            try { if (coordinator is not null) await coordinator.DisposeAsync(); }
            finally
            {
                run.PullReads = coordinator is null ? 0 : coordinator.Reads - readsAtStart;
                run.PullGapHolds = coordinator?.GapHolds ?? 0;
                run.PullGapRetries = coordinator?.GapRetries ?? 0;
                run.PullMaxGapHoldMs = coordinator?.MaxGapHoldMs ?? 0;
                run.PullEventSample = source?.Sample;
                run.PullProgressDocument = progress?.FinalDocument;
                run.PullCounts = progress?.FinalCounts ?? [];
                run.PullFlushes = progress?.Flushes ?? 0;
                run.PullCountDocuments = progress?.CountDocuments ?? 0;
                if (cpuBefore.HasValue) await CaptureContainerCpu(mongo, run, cpuBefore.Value);
            }
        }
    }
}
