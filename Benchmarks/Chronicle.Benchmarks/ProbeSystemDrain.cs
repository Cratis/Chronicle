// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MongoDB.Bson;

namespace Cratis.Chronicle.Benchmarks;

internal sealed record ProbeSystemDrainObserver(
    string Id,
    string EventSequenceId,
    ulong? Tail,
    decimal? InitialNextSequenceNumber,
    decimal? InitialLastHandledSequenceNumber,
    decimal? NextSequenceNumber,
    decimal? LastHandledSequenceNumber,
    int? RunningState,
    string Status,
    double? ReachedTailMs,
    DateTimeOffset? ReachedTailUtc,
    DateTimeOffset ObservedUtc,
    string? State);

internal static class ProbeSystemDrain
{
    internal static async Task Run(ProbeMongo mongo, ProbeRun run, CancellationToken cancellation)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        budget.CancelAfter(TimeSpan.FromSeconds(run.DrainBudgetSeconds));
        var token = budget.Token;
        var timer = Stopwatch.StartNew();
        run.SystemDrainStartedUtc = DateTimeOffset.UtcNow;
        run.SystemDrainStatus = "RUNNING";
        try
        {
            var definitions = (await mongo.Observers().WaitAsync(token))
                .Where(d => Probe.SystemObserverIds.Contains(d["_id"].AsString)).ToArray();
            if (definitions.Length != Probe.SystemObserverIds.Length)
                throw new InvalidOperationException($"Expected {Probe.SystemObserverIds.Length} system observers, found {definitions.Length}.");
            var tails = new Dictionary<string, ulong?> { ["event-log"] = run.Appended.Keys.Max(p => p.SequenceNumber) };
            foreach (var sequence in definitions.Select(d => d["eventSequenceId"].AsString).Distinct())
                if (!tails.ContainsKey(sequence)) tails[sequence] = await mongo.SequenceTail(sequence).WaitAsync(token);
            // Retain even absent states explicitly, rather than losing the population on a deadline.
            run.SystemDrainObservers = definitions.Select(d => new ProbeSystemDrainObserver(
                d["_id"].AsString, d["eventSequenceId"].AsString, tails[d["eventSequenceId"].AsString],
                null, null, null, null, null, "NOT_OBSERVED", null, null, DateTimeOffset.UtcNow, null)).ToArray();
            while (true)
            {
                var states = await mongo.State().WaitAsync(token);
                var observed = DateTimeOffset.UtcNow;
                run.SystemDrainObservers = run.SystemDrainObservers.Select(prior =>
                {
                    var state = states.SingleOrDefault(s => s["_id"].AsString == prior.Id);
                    var next = Number(state, "nextEventSequenceNumber");
                    var last = Number(state, "lastHandledEventSequenceNumber");
                    var reached = state is not null && (!prior.Tail.HasValue ||
                        Probe.StateAtTail(states, prior.Tail.Value, prior.Id, allowScanned: true));
                    return prior with
                    {
                        InitialNextSequenceNumber = prior.Status == "NOT_OBSERVED" ? next : prior.InitialNextSequenceNumber,
                        InitialLastHandledSequenceNumber = prior.Status == "NOT_OBSERVED" ? last : prior.InitialLastHandledSequenceNumber,
                        NextSequenceNumber = next,
                        LastHandledSequenceNumber = last,
                        RunningState = state?.GetValue("runningState", BsonNull.Value) is { IsBsonNull: false } running ? running.ToInt32() : null,
                        Status = state is null ? "NO_STATE" : reached ? "AT_TAIL" : "DRAINING",
                        ReachedTailMs = prior.ReachedTailMs ?? (reached ? timer.Elapsed.TotalMilliseconds : null),
                        ReachedTailUtc = prior.ReachedTailUtc ?? (reached ? observed : null),
                        ObservedUtc = observed,
                        State = state?.ToJson()
                    };
                }).ToArray();
                // Preserve the existing drain gate: every system observer with state must reach its own sequence tail.
                if (run.SystemDrainObservers.All(d => d.Status is "AT_TAIL" or "NO_STATE"))
                {
                    run.SystemDrainStatus = "COMPLETE";
                    return;
                }
                await Task.Delay(100, token);
            }
        }
        catch (OperationCanceledException)
        {
            run.SystemDrainStatus = "DEADLINE";
            throw;
        }
        catch
        {
            run.SystemDrainStatus = "INVALID";
            throw;
        }
        finally
        {
            run.SystemDrainMs = timer.Elapsed.TotalMilliseconds;
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                systemDrain = run.SystemDrainStatus,
                elapsedMs = run.SystemDrainMs,
                seededEvents = run.Expected,
                drainWindowSeededEventsPerSecond = run.SystemDrainSeededEventsPerSecond,
                seedToTailEventsPerSecond = run.SystemSeedToTailEventsPerSecond,
                observers = run.SystemDrainObservers
            }));
        }
    }

    static decimal? Number(BsonDocument? state, string field) => state?.GetValue(field, BsonNull.Value) is { IsBsonNull: false } value
        ? value.ToDecimal() : null;
}
