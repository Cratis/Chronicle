// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using MongoDB.Bson;

namespace Cratis.Chronicle.Benchmarks;

internal static class Probe
{
    static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    internal static readonly string[] SystemObserverIds =
    [
        "$system.Cratis.Chronicle.Observation.Webhooks.WebhookReactor",
        "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
        "$system.Cratis.Chronicle.EventSequences.Migrations.EventTypeGenerationMigrationReactor",
        "$system.Cratis.Chronicle.EventSequences.EventSequencesReactor",
        "$system.patterns"
    ];

    internal static async Task<int> Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "self-test") return SelfTest();
        if (args.Length != 1) throw new ArgumentException("Supply exactly one scenario per phase.");
        if (args[0].Split(':').Contains("noop")) throw new ArgumentException("Noop rows were removed; use measured handler busy time and CPU headroom.");
        var output = Environment.GetEnvironmentVariable("PROBE_OUT") ?? throw new InvalidOperationException("PROBE_OUT is required.");
        var run = new ProbeRun(args[0], $"measure4748-{Guid.NewGuid():N}");
        Directory.CreateDirectory(output);
        var fixture = new ChronicleBenchmarkFixture();
        ProbeMongo? mongo = null;
        if (run.FinalizationReserveSeconds <= 0 || run.MeasurementDeadlineSeconds <= 0 || run.DrainBudgetSeconds <= 0)
            throw new ArgumentException("Deadline, finalization reserve and drain budget must be positive, with reserve below deadline.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(run.MeasurementDeadlineSeconds));
        using var process = Process.GetCurrentProcess();
        Task? worker = null;
        try
        {
            // The watchdog also bounds synchronous client construction and registration, not only delivery polling.
            worker = Task.Run(async () =>
            {
                await fixture.Prepare(deadline.Token);
                mongo = new ProbeMongo(fixture.Container, run.EventStore, run.ProfilerCapacityBytes);
                await Measure(fixture, mongo, run, deadline.Token);
            });
            await worker.WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            run.Status = run.RegisteredMs.HasValue ? "DID_NOT_FINISH" : "INVALID";
            run.Reason += !run.RegisteredMs.HasValue ? $"setup-deadline: {run.Phase}" :
                $"Measurement deadline {run.MeasurementDeadlineSeconds}s reached (total internal budget {run.DeadlineSeconds}s, finalization reserve {run.FinalizationReserveSeconds}s); acknowledged membership only.";
        }
        catch (Exception error)
        {
            run.Status = "INVALID";
            run.Reason = error is TimeoutException && !run.RegisteredMs.HasValue
                ? $"setup-deadline: {run.Phase}\n{error}" : error.ToString();
        }
        finally
        {
            if (worker is not null && !worker.IsCompleted)
            {
                try { await worker.WaitAsync(TimeSpan.FromSeconds(30)); }
                catch (Exception error) { run.Reason += $"\nWorker finalization: {error.Message}"; }
                if (!worker.IsCompleted) { run.Status = "INVALID"; run.Reason += "\nWorker did not stop before snapshot."; }
            }
            run.Clock.Stop();
            run.Snapshot();
            run.ClientCpuMs = run.Started == 0 ? 0 : (process.TotalProcessorTime - run.CpuStart).TotalMilliseconds;
            // Reserve time before the phase timeout for profiling, partial output and owned-resource disposal.
            if (mongo is not null && run.ProfileStart.HasValue)
            {
                try
                {
                    run.PersistedObserverState = (await mongo.State().WaitAsync(TimeSpan.FromSeconds(5))).Select(d => d.ToJson()).ToArray();
                    run.PersistedJobs = (await mongo.Jobs().WaitAsync(TimeSpan.FromSeconds(5))).Select(d => d.ToJson()).ToArray();
                    if (run.Scenario.StartsWith("catchup") && run.DurableMs.HasValue &&
                        run.PersistedJobs.Select(BsonDocument.Parse).Any(j => j.GetValue("details", "") == run.ProbeObserverId))
                        throw new InvalidOperationException("Non-deleted probe job remains after claimed catch-up completion.");
                    run.EndObservers = (await mongo.Observers().WaitAsync(TimeSpan.FromSeconds(5))).Select(d => d.ToJson()).ToArray();
                    CheckObservers(run.EndObservers.Select(BsonDocument.Parse).ToArray(), run.Scenario.StartsWith("appendonly") ? 0 : 1, run.Scenario.Contains("projection"));
                    run.Mongo = await mongo.EndProfile().WaitAsync(TimeSpan.FromSeconds(60));
                    ValidateProfile(run.Mongo.Value);
                }
                catch (Exception error)
                {
                    run.Status = "INVALID";
                    run.Reason = $"{run.Reason}\nFinal attribution/observer check: {error}";
                }
            }
            if (run.Expected == 0 || !run.ObserverListPassed || !run.Mongo.HasValue)
            {
                run.Status = "INVALID";
                run.Reason = $"{run.Reason}\nNon-vacuity: expected={run.Expected}, observerList={run.ObserverListPassed}, profile={run.Mongo.HasValue}.";
            }
            if (!run.AppendFinished)
            {
                run.Status = "INVALID";
                run.Reason = $"{run.Reason}\nAppend phase incomplete: outstanding RPC outcomes may be unknown; partial acknowledged membership only.";
            }
            try
            {
                run.KernelLogFile = $"{run.EventStore}-kernel.log";
                File.WriteAllText(Path.Combine(output, run.KernelLogFile), await fixture.KernelLogs().WaitAsync(TimeSpan.FromSeconds(20)));
            }
            catch (Exception error)
            {
                run.KernelLogFile = null;
                run.Reason += $"\nKernel log capture failed: {error.Message}";
            }
            run.DeliveriesFile = $"{run.EventStore}-deliveries.json";
            File.WriteAllText(Path.Combine(output, run.DeliveriesFile), JsonSerializer.Serialize(run.Delivery.FirstDeliveries(run.Started), JsonOptions));
            File.WriteAllText(Path.Combine(output, $"{run.EventStore}-expected.json"), JsonSerializer.Serialize(
                run.Appended.OrderBy(p => p.Key.SequenceNumber).Select(p => new { position = p.Key, payload = p.Value }), JsonOptions));
            // A per-store partial is available before disposal; the authoritative JSONL row follows cleanup.
            File.WriteAllText(Path.Combine(output, $"{run.EventStore}-partial.json"), JsonSerializer.Serialize(run, JsonOptions));
            try
            {
                await fixture.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(20));
            }
            catch (Exception error)
            {
                File.AppendAllText(Path.Combine(output, "cleanup-errors.log"), $"{run.EventStore}: {error}\n");
                Console.Error.WriteLine($"Owned fixture cleanup incomplete: {error}");
                run.Status = "INVALID";
                run.Reason += $"\nOwned fixture cleanup incomplete: {error.Message}";
            }
            // Final snapshot is after quiescence, client shutdown and cleanup; late deliveries cannot silently pass.
            run.Snapshot();
            if (run.RegisteredMs.HasValue && run.Accounting is { } accounting && run.Expected > 0 &&
                (accounting.Duplicates != 0 || accounting.FirstDeliveryOrderViolations != 0 ||
                 accounting.Unexpected.Length != 0 || accounting.Missing.Length != 0))
            {
                run.Status = "DEFECT";
                run.Reason += $"\nDelivery defect: missing={accounting.Missing.Length}, duplicates={accounting.Duplicates}, order={accounting.FirstDeliveryOrderViolations}, unexpected={accounting.Unexpected.Length}.";
            }
            Save(output, run);
        }
        return run.Status switch { "COMPLETE" => 0, "DEFECT" => 1, _ => 2 };
    }

    static void Save(string output, ProbeRun result)
    {
        var json = JsonSerializer.Serialize(result, JsonOptions);
        File.AppendAllText(Path.Combine(output, "results.jsonl"), json + Environment.NewLine);
        Console.WriteLine("RESULT " + json);
    }

    static async Task Measure(ChronicleBenchmarkFixture fixture, ProbeMongo mongo, ProbeRun run, CancellationToken cancellation)
    {
        var parts = run.Scenario.Split(':');
        var catchup = parts[0] == "catchup";
        var appendOnly = parts[0] == "appendonly";
        var projection = parts[1] == "projection";
        var n = int.Parse(parts[2]);
        var keys = int.Parse(parts[3]);
        var concurrency = int.Parse(parts[4]);
        var batch = parts.Contains("batch");
        run.Requested = n;
        var observer = projection ? typeof(BenchmarkProjection) : typeof(ProbeReactor);
        run.Phase = "seeder-connection";
        using var seeder = new ChronicleClientHelper(fixture, run.EventStore, new ObserverScopedClientArtifactsProvider());
        await seeder.WaitForConnection().WaitAsync(cancellation);
        CheckObservers(await mongo.Observers().WaitAsync(cancellation), 0, projection);
        if (catchup)
        {
            run.Phase = "seeding";
            var timer = Stopwatch.StartNew();
            await Append(seeder.EventLog, run, n, keys, concurrency, batch, cancellation);
            run.AppendMs = timer.Elapsed.TotalMilliseconds;
            run.Phase = "system-drain";
            await ProbeSystemDrain.Run(mongo, run, cancellation);
        }
        run.Phase = "live-warmup";
        using var live = !catchup && !appendOnly
            ? new ChronicleClientHelper(fixture, run.EventStore, new ObserverScopedClientArtifactsProvider(observer)) : null;
        if (!catchup)
        {
            await (live ?? seeder).WaitForConnection().WaitAsync(cancellation);
            await Warm(live ?? seeder, mongo, !appendOnly ? run.ProbeObserverId : null, cancellation);
        }
        run.Phase = "profile-setup";
        var before = await mongo.Observers().WaitAsync(cancellation);
        run.BeforeObservers = before.Select(d => d.ToJson()).ToArray();
        run.InitialObserverState = (await mongo.State().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
        CheckObservers(before, catchup || appendOnly ? 0 : 1, projection);
        run.ProfileStart = await mongo.StartProfile(run.ProbeObserverId).WaitAsync(cancellation);
        ProbeDelivery.Current = run.Delivery;
        run.Started = Stopwatch.GetTimestamp();
        run.CpuStart = Process.GetCurrentProcess().TotalProcessorTime;
        run.Clock.Start(); // Catch-up clocks deliberately exclude seeding and container startup.
        run.Phase = "probe-registration";
        using var catchingUp = catchup
            ? new ChronicleClientHelper(fixture, run.EventStore, new ObserverScopedClientArtifactsProvider(observer)) : null;
        if (catchingUp is not null)
        {
            await catchingUp.WaitForConnection().WaitAsync(cancellation);
            // An append-ready connection does not imply the asynchronous reactor subscription is persisted.
            var registration = Stopwatch.StartNew();
            while (true)
            {
                if ((await mongo.Observers().WaitAsync(cancellation)).Any(o => o["_id"].AsString == run.ProbeObserverId))
                {
                    run.RegisteredMs = run.Clock.Elapsed.TotalMilliseconds;
                    await mongo.MarkRegistered().WaitAsync(cancellation);
                    break;
                }
                if (registration.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Probe observer definition was not persisted within 30 seconds.");
                await Task.Delay(100, cancellation);
            }
        }
        var registered = await mongo.Observers().WaitAsync(cancellation);
        run.Observers = registered.Select(d => d.ToJson()).ToArray();
        CheckObservers(registered, appendOnly ? 0 : 1, projection);
        run.ObserverListPassed = true;
        if (!run.RegisteredMs.HasValue)
        {
            run.RegisteredMs = run.Clock.Elapsed.TotalMilliseconds;
            await mongo.MarkRegistered().WaitAsync(cancellation);
        }
        run.Phase = "measurement";
        if (!catchup)
        {
            await Append((live ?? seeder).EventLog, run, n, keys, concurrency, batch, cancellation);
            run.AppendMs = run.Clock.Elapsed.TotalMilliseconds;
        }
        if (run.Expected != n) throw new InvalidOperationException($"Expected {n} unique acknowledged append results, got {run.Expected}.");
        var tail = run.Appended.Keys.Max(p => p.SequenceNumber);
        if (appendOnly) run.EffectMs = run.AppendMs;
        try
        {
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                run.Snapshot();
                var allState = await mongo.State().WaitAsync(cancellation);
                run.PersistedObserverState = allState.Select(d => d.ToJson()).ToArray();
                if (StateAtTail(allState, tail, "$system.patterns")) run.SystemDurableMs ??= run.Clock.Elapsed.TotalMilliseconds;
                // Same explicit relaxation as the drain (PROBE_DRAIN_ALLOW_INFLIGHT=1): progress at tail with persisted
                // in-flight partitions that never clear. Recorded as run.SystemDrainStatus COMPLETE_WITH_INFLIGHT.
                else if (Environment.GetEnvironmentVariable("PROBE_DRAIN_ALLOW_INFLIGHT") == "1" &&
                    StateAtTail(allState, tail, "$system.patterns", allowInFlight: true))
                {
                    run.SystemDurableMs ??= run.Clock.Elapsed.TotalMilliseconds;
                    run.SystemDrainStatus = "COMPLETE_WITH_INFLIGHT";
                }
                if (!appendOnly)
                {
                    var state = allState.Where(s => s["_id"].AsString == run.ProbeObserverId).ToArray();
                    var jobs = (await mongo.Jobs().WaitAsync(cancellation))
                        .Where(j => j.GetValue("details", "").ToString() == run.ProbeObserverId).ToArray();
                    run.PersistedJobs = jobs.Select(d => d.ToJson()).ToArray();
                    // Retained jobs must name this sole observer and the catch-up job type, not merely have a success code.
                    var jobIdentityVerified = jobs.All(j => j.Contains("status") &&
                        j.ToJson().Contains("CatchUpObserver", StringComparison.Ordinal) &&
                        j.ToJson().Contains(projection ? typeof(BenchmarkProjection).FullName! : "probe-4748", StringComparison.Ordinal));
                    if (catchup && !jobIdentityVerified) throw new InvalidOperationException("Cannot verify retained catch-up job identity/schema for the sole probe observer.");
                    // Read the sink in the same iteration as the observer state so the effect clock is not one loop late.
                    if (projection)
                    {
                        var rows = await mongo.ProjectionRows().WaitAsync(cancellation);
                        run.ProjectionDocuments = new(mongo.ProjectionEvidence);
                        run.ProjectionRows = rows.Length;
                        run.ProjectionValuesMatch = ProjectionMatches(rows, run);
                        if (run.ProjectionValuesMatch.Value) run.EffectMs ??= run.Clock.Elapsed.TotalMilliseconds;
                    }
                    var finishedJobs = false;
                    if (catchup && StateAtTail(state, tail, run.ProbeObserverId) && jobs.Length == 0 && !run.JobMs.HasValue)
                    {
                        var completed = await mongo.CompletedCatchupJobs(run.ProbeObserverId, tail).WaitAsync(cancellation);
                        run.CompletedJobEvidence = completed.Select(d => d.ToJson()).ToArray();
                        run.JobWriteSamples = mongo.JobWriteSamples;
                        if (completed.Length != 1) throw new InvalidOperationException("Tail reached but full handled-all/tail job evidence plus deletion is not proven for exactly one job.");
                        finishedJobs = true;
                    }
                    var terminal = catchup ? jobs.FirstOrDefault(j => j.GetValue("status", 0).ToInt32() >= 6) : null;
                    if (terminal is not null)
                    {
                        // A non-successful terminal job never becomes healthy; capture evidence while the client is still connected.
                        run.JobTerminalMs = run.Clock.Elapsed.TotalMilliseconds;
                        run.JobTerminalStatus = terminal["status"].ToInt32();
                        run.JobTerminalObservedUtc = DateTimeOffset.UtcNow;
                        await Task.Delay(5000, cancellation);
                        await CaptureConnectedFailure(mongo, run, cancellation);
                        run.Status = "JOB_FAILED";
                        run.Reason = $"Catch-up job reached non-successful terminal status {run.JobTerminalStatus}; evidence captured with the client connected.";
                        return;
                    }
                    if (run.JobMs.HasValue && jobs.Length == 0) finishedJobs = true;
                    if (catchup && finishedJobs) run.JobMs ??= run.Clock.Elapsed.TotalMilliseconds;
                    if (StateAtTail(state, tail, run.ProbeObserverId) && (!catchup || finishedJobs)) run.DurableMs ??= run.Clock.Elapsed.TotalMilliseconds;
                }
                if ((appendOnly || (run.DurableMs.HasValue && (!projection || run.EffectMs.HasValue))) && run.SystemDurableMs.HasValue)
                {
                    await Task.Delay(2000, cancellation);
                    run.Status = "COMPLETE";
                    return;
                }
                await Task.Delay(100, cancellation);
            }
        }
        finally
        {
            if (catchup && !run.DurableMs.HasValue && run.ConnectedObserverState.Length == 0)
            {
                // Measure owns the connected client: snapshot before its using declaration disposes it,
                // even when the measurement token expired with a job still Running (status below 6).
                using var captureDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                try { await CaptureConnectedFailure(mongo, run, captureDeadline.Token); }
                catch (Exception error) { run.Reason += $"Connected failure capture incomplete: {error.Message}\n"; }
            }
        }
    }

    static async Task CaptureConnectedFailure(ProbeMongo mongo, ProbeRun run, CancellationToken cancellation)
    {
        run.Snapshot();
        run.ConnectedObserverState = (await mongo.State().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
        run.PersistedJobs = (await mongo.Jobs().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
        var steps = await mongo.JobSteps().WaitAsync(cancellation);
        run.JobStepCount = steps.Length;
        run.UnsuccessfulJobSteps = steps.Where(d => d.GetValue("status", 0).ToInt32() != 3).Select(d => d.ToJson()).ToArray();
        run.FailedJobSteps = (await mongo.FailedJobSteps().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
        run.FailedPartitions = (await mongo.FailedPartitions().WaitAsync(cancellation)).Select(d => d.ToJson()).ToArray();
    }

    static async Task Append(IEventLog log, ProbeRun run, int n, int keyCount, int concurrency, bool batch, CancellationToken cancellation)
    {
        var keys = Enumerable.Range(0, keyCount).Select(_ => EventSourceId.New()).ToArray();
        var payloads = Enumerable.Range(0, n).Select(i => new BenchmarkEvent(keys[i % keyCount].Value, i, DateTimeOffset.UtcNow)).ToArray();
        if (batch)
        {
            run.Submitted = n;
            var result = await log.AppendMany(payloads.Select((p, i) => new EventForEventSourceId(keys[i % keyCount], p)),
                concurrencyScopes: keys.ToDictionary(key => key, _ => ConcurrencyScope.None)).WaitAsync(cancellation);
            if (!result.IsSuccess || result.SequenceNumbers.Count() != n) throw new InvalidOperationException(JsonSerializer.Serialize(result));
            foreach (var (sequence, i) in result.SequenceNumbers.Select((s, i) => (s, i)))
                AddExpected(run, keys[i % keyCount], sequence.Value, payloads[i]);
        }
        else
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, n), new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = cancellation }, async (i, token) =>
            {
                Interlocked.Increment(ref run.SubmittedCount);
                var key = keys[i % keyCount];
                var result = await log.Append(key, payloads[i], concurrencyScope: ConcurrencyScope.None).WaitAsync(token);
                if (!result.IsSuccess || !result.SequenceNumber.IsActualValue) throw new InvalidOperationException(JsonSerializer.Serialize(result));
                AddExpected(run, key, result.SequenceNumber.Value, payloads[i]);
            });
        }
        run.AppendFinished = true;
    }

    static void AddExpected(ProbeRun run, EventSourceId key, ulong sequence, BenchmarkEvent payload)
    {
        if (!run.Appended.TryAdd(new(key.Value, sequence), new(payload.Value, payload.Timestamp)))
            throw new InvalidOperationException($"Duplicate append result {key}@{sequence}.");
    }

    static async Task Warm(ChronicleClientHelper helper, ProbeMongo mongo, string? observerId, CancellationToken cancellation)
    {
        var result = await helper.EventLog.Append(EventSourceId.New(), new BenchmarkEvent("warm", -1, DateTimeOffset.UtcNow)).WaitAsync(cancellation);
        if (!result.IsSuccess) throw new InvalidOperationException("Warmup append failed.");
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(30))
        {
            var states = await mongo.State().WaitAsync(cancellation);
            if (StateAtTail(states, result.SequenceNumber.Value, "$system.patterns") &&
                (observerId is null || StateAtTail(states, result.SequenceNumber.Value, observerId))) return;
            await Task.Delay(100, cancellation);
        }
        throw new TimeoutException("Warmup observer did not persist progress within 30 seconds.");
    }

    static void CheckObservers(BsonDocument[] observers, int count, bool projection)
    {
        var expectedId = projection ? typeof(BenchmarkProjection).FullName! : "probe-4748";
        var expected = SystemObserverIds.Concat(count == 1 ? [expectedId] : Array.Empty<string>()).ToHashSet(StringComparer.Ordinal);
        var actual = observers.Select(o => o["_id"].AsString).ToArray();
        if (actual.Length != expected.Count || !actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new InvalidOperationException($"Observer-list check expected {string.Join(',', expected)}: {string.Join(';', observers.Select(d => d.ToJson()))}");
    }

    internal static bool StateAtTail(BsonDocument[] states, ulong tail, string observerId, bool allowScanned = false, bool allowInFlight = false)
    {
        var state = states.SingleOrDefault(s => s["_id"].AsString == observerId);
        var progress = state?.GetValue("lastHandledEventSequenceNumber", -1).ToDecimal() ?? -1;
        var next = state?.GetValue("nextEventSequenceNumber", -1).ToDecimal() ?? -1;
        var reached = progress >= tail && progress < (decimal)(ulong.MaxValue - 2);
        // System observers on the system sequence can exclude every event: actual scanned-next is then the progress proof.
        reached |= allowScanned && next > tail && next < (decimal)(ulong.MaxValue - 2);
        return state is not null && reached &&
            state.GetValue("runningState", 0).ToInt32() == 1 &&
            state.GetValue("failedPartitionCount", 0).ToInt32() == 0 &&
            state.GetValue("catchingUpPartitions", new BsonArray()).AsBsonArray.Count == 0 &&
            (allowInFlight || state.GetValue("inFlightPartitions", new BsonArray()).AsBsonArray.Count == 0);
    }

    static bool ProjectionMatches(BsonDocument[] rows, ProbeRun run)
    {
        var latest = run.Appended.GroupBy(p => p.Key.Key).ToDictionary(g => g.Key, g => g.MaxBy(p => p.Key.SequenceNumber).Value);
        var measured = rows.Where(r => latest.ContainsKey(r.GetValue("_id", "").AsString)).ToArray();
        return measured.Length == latest.Count && measured.Select(r => r["_id"].AsString).Distinct().Count() == latest.Count &&
            measured.All(r => r.GetValue("Name", "").AsString == r["_id"].AsString &&
                r.GetValue("Value", -1).ToInt32() == latest[r["_id"].AsString].Value &&
                r.GetValue("Timestamp", BsonNull.Value).IsBsonDateTime &&
                r["Timestamp"].AsBsonDateTime.MillisecondsSinceEpoch == latest[r["_id"].AsString].Timestamp.ToUnixTimeMilliseconds());
    }

    static void ValidateProfile(JsonElement profile)
    {
        if (!profile.GetProperty("boundary").EnumerateArray().All(b => b.GetProperty("retained").GetBoolean() && b.GetProperty("registeredRetained").GetBoolean()) || profile.GetProperty("commands").GetInt64() == 0)
            throw new InvalidOperationException("Profiler boundary lost or no scoped Mongo operations recorded.");
        if (profile.GetProperty("unparsed").GetInt64() != 0)
            throw new InvalidOperationException("Profiler rows could not be parsed; INVALID, not silently omitted.");
        var committed = profile.GetProperty("serverCommittedDelta").GetInt64();
        var aborted = profile.GetProperty("serverAbortedDelta").GetInt64();
        if (profile.GetProperty("transactionIdentityMissing").GetInt64() != 0 || committed < 0 || aborted != 0 ||
            profile.GetProperty("transactionsObserved").GetInt64() > committed ||
            profile.GetProperty("terminalCommandsProfiled").GetInt64() > committed + aborted)
            throw new InvalidOperationException("Run-bounded transaction cross-check failed: missing identity, abort, counter regression, or observed transactions exceed commits.");
        if (profile.GetProperty("transactions").EnumerateArray().Any(t => t.GetProperty("owner").GetString() == "uncorrelated"))
            throw new InvalidOperationException("Admin transaction commands could not be correlated by session/txnNumber to run databases; INVALID, not silently attributed.");
    }

    static int SelfTest()
    {
        var positions = new[] { new ProbePosition("key", 10), new ProbePosition("key", 20), new ProbePosition("key", 30) };
        var delivery = new ProbeDelivery { Drop = true, DropIndex = 1 };
        var start = Stopwatch.GetTimestamp();
        delivery.Record(positions[2], 2);
        delivery.Record(positions[1], 1);
        delivery.Record(positions[0], 0);
        delivery.Record(positions[0], 0);
        delivery.Record(new("wrong-key", 20), 4);
        delivery.Record(new("key", 99), 5);
        var result = delivery.Snapshot(positions, start);
        var runningMax = new ProbeDelivery();
        runningMax.Record(positions[2], -1); // Warm-like negative values are not dropped without explicit Drop.
        runningMax.Record(positions[0], 0);
        runningMax.Record(positions[1], 1);
        var orderCheck = runningMax.Snapshot(positions, start);
        var passed = orderCheck.DistinctDelivered == 3 && orderCheck.FirstDeliveryOrderViolations == 2 && result.Expected == 3 && result.DistinctDelivered == 2 && result.Duplicates == 1 &&
            result.FirstDeliveryOrderViolations == 1 && result.Missing.SequenceEqual([positions[1]]) && result.EffectMs is null &&
            result.Unexpected.ToHashSet().SetEquals([new("wrong-key", 20), new("key", 99)]);
        Console.WriteLine(JsonSerializer.Serialize(new { selfTest = "explicit drop, duplicate, running-max order, unexpected sequence, wrong key, negative warm payload", passed, result, orderCheck }, JsonOptions));
        return passed ? 0 : 1;
    }
}
