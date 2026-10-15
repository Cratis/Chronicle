// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks;

internal static class ProbePullSelfTest
{
    sealed class Source(PullEvent[] events, Func<ulong, bool>? visible = null) : IPullPageSource
    {
        public Task<PullEvent[]> Read(PullCursor? cursor, int page, CancellationToken cancellation) => Task.FromResult(events
            .Where(e => (cursor is null || e.Sequence > cursor.Position) && (visible?.Invoke(e.Sequence) ?? true)).Take(page).ToArray());
    }

    sealed class Progress(ConcurrentDictionary<ulong, byte> completed, bool check = true) : IPullProgress
    {
        internal readonly ConcurrentQueue<ulong> Saved = new();
        public Task Save(PullPrefix prefix, IReadOnlyDictionary<string, long> counts, CancellationToken cancellation)
        {
            if (prefix.W is not { } w) throw new InvalidOperationException("Saved before-first as a real position.");
            if (check)
                for (ulong i = 0; i <= w; i++)
                    if (!completed.ContainsKey(i)) throw new InvalidOperationException($"Saved {w} across incomplete {i}.");
            Saved.Enqueue(w);
            return Task.CompletedTask;
        }
    }

    static PullEvent Event(ulong seq, string key) => new(seq, key, ProbePullMongoSource.EventType, new((int)seq, DateTimeOffset.UnixEpoch.AddMilliseconds(seq)));
    static ProbePullOptions Options(ulong? drop = null) => new(64, 1000, 1, "equivalent", "hint", drop);
    static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    static async Task Until(Func<bool> condition, ProbePullCoordinator coordinator, CancellationToken cancellation)
    {
        while (!condition()) { coordinator.CheckHealth(); await Task.Delay(1, cancellation); }
        coordinator.CheckHealth();
    }

    internal static async Task<int> Run()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = deadline.Token;
        var passed = new List<string>();
        try
        {
            var completed = new ConcurrentDictionary<ulong, byte>();
            var progress = new Progress(completed);
            var block = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (var coordinator = new ProbePullCoordinator(new Source([Event(0, "zero")]), progress, Options(), async e =>
            { entered.SetResult(); await block.Task.WaitAsync(token); completed[e.Sequence] = 1; }, token))
            {
                await entered.Task.WaitAsync(token);
                await Task.Delay(20, token);
                Assert(progress.Saved.IsEmpty, "W must remain None while event zero is blocked.");
                block.SetResult();
                await Until(() => progress.Saved.Contains(0UL), coordinator, token);
            }
            passed.Add("event zero/before-first");

            completed = new();
            progress = new(completed);
            block = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var blockedKey = "blocked";
            var otherKey = Enumerable.Range(0, 100).Select(i => $"other-{i}").First(k => ProbePullCoordinator.Lane(k, 64) != ProbePullCoordinator.Lane(blockedKey, 64));
            var events = Enumerable.Range(0, 10002).Select(i => Event((ulong)i, i == 1 ? blockedKey : otherKey)).ToArray();
            await using (var coordinator = new ProbePullCoordinator(new Source(events), progress, Options(), async e =>
            { if (e.Sequence == 1) await block.Task.WaitAsync(token); completed[e.Sequence] = 1; }, token))
            {
                await Until(() => completed.Count == 10001, coordinator, token);
                await Task.Delay(20, token);
                Assert(progress.Saved.All(w => w < 1), "Watermark passed blocked seq 1 after 10k later completions.");
                block.SetResult();
                await Until(() => progress.Saved.Contains(10001UL), coordinator, token);
            }
            passed.Add("blocked handler + 10k later completions, independent prefix check at EVERY save");

            completed = new();
            progress = new(completed);
            var orders = new ConcurrentDictionary<string, List<ulong>>();
            events = Enumerable.Range(0, 512).Select(i => Event((ulong)i, $"key-{i % 31}")).ToArray();
            await using (var coordinator = new ProbePullCoordinator(new Source(events), progress, Options(), async e =>
            {
                // Seeded pseudo-random delays, deterministic and independent of scheduling.
                await Task.Delay((int)((e.Sequence * 1103515245 + 12345) % 4), token);
                orders.GetOrAdd(e.Key, _ => []).Add(e.Sequence);
                completed[e.Sequence] = 1;
            }, token))
                await Until(() => progress.Saved.Contains(511UL), coordinator, token);
            Assert(orders.All(p => p.Value.SequenceEqual(p.Value.Order())), "Per-key lane order violated.");
            passed.Add("randomized delays + per-key order + independent completion-set check");

            completed = new();
            progress = new(completed);
            var reveal = false;
            events = Enumerable.Range(0, 10).Select(i => Event((ulong)i, "gap")).ToArray();
            await using (var coordinator = new ProbePullCoordinator(new Source(events, s => s != 1 || Volatile.Read(ref reveal)), progress, Options(), e =>
            { completed[e.Sequence] = 1; return Task.CompletedTask; }, token))
            {
                await Until(() => coordinator.GapHolds > 2, coordinator, token);
                Assert(completed.Keys.All(s => s == 0) && progress.Saved.All(w => w == 0), "Reader crossed a visibility gap.");
                Volatile.Write(ref reveal, true);
                await Until(() => progress.Saved.Contains(9UL), coordinator, token);
                Assert(coordinator.GapHolds > 0, "Gap script was vacuous.");
            }
            passed.Add("delayed visibility gap held, reread, never crossed");

            completed = new();
            progress = new(completed, false);
            var delivery = new ProbeDelivery();
            events = Enumerable.Range(0, 10).Select(i => Event((ulong)i, "drop")).ToArray();
            var started = Stopwatch.GetTimestamp();
            await using (var coordinator = new ProbePullCoordinator(new Source(events), progress, Options(3), e =>
            { delivery.Record(new(e.Key, e.Sequence), e.Payload!.Value, e.Payload); return Task.CompletedTask; }, token))
                await Until(() => progress.Saved.Contains(9UL), coordinator, token);
            var payloads = events.ToDictionary(e => new ProbePosition(e.Key, e.Sequence), e => e.Payload!);
            var accounting = delivery.Snapshot(payloads.Keys.ToArray(), started, payloads);
            Assert(accounting.Missing.SequenceEqual([new ProbePosition("drop", 3)]) && accounting.Duplicates == 0 && accounting.Unexpected.Length == 0 && accounting.PayloadMismatches == 0,
                "Planted skipped dispatch must be exactly missing seq 3 even though checkpoint reaches tail.");
            var badPayload = new ProbeDelivery();
            badPayload.Record(new("drop", 0), 0, new(99, DateTimeOffset.UnixEpoch));
            Assert(badPayload.Snapshot([new("drop", 0)], started, payloads).PayloadMismatches == 1, "Payload detector failed planted corruption.");
            passed.Add("drop reported missing despite checkpoint tail; payload corruption detected");

            completed = new();
            progress = new(completed);
            var completionTime = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (var coordinator = new ProbePullCoordinator(new Source([Event(0, "lull")]), progress, Options() with { FlushMs = 80 }, e =>
            { completed[e.Sequence] = 1; completionTime.SetResult(); return Task.CompletedTask; }, token))
            {
                await completionTime.Task.WaitAsync(token);
                await Task.Delay(20, token);
                Assert(progress.Saved.IsEmpty, "Idle reader forced an early flush before cadence.");
                await Until(() => progress.Saved.Contains(0UL), coordinator, token);
            }
            passed.Add("live lull does not force idle flush; normal timer persists it");

            completed = new();
            progress = new(completed);
            events = Enumerable.Range(0, 20).Select(i => i % 10 == 9 ? Event((ulong)i, "selected") : new PullEvent((ulong)i, "other", "unrelated", null)).ToArray();
            foreach (var evt in events.Where(e => e.Payload is null)) completed[evt.Sequence] = 1;
            var selected = new ConcurrentQueue<ulong>();
            await using (var coordinator = new ProbePullCoordinator(new Source(events), progress, Options(), e =>
            { selected.Enqueue(e.Sequence); completed[e.Sequence] = 1; return Task.CompletedTask; }, token))
                await Until(() => progress.Saved.Contains(19UL), coordinator, token);
            Assert(selected.SequenceEqual([9UL, 19UL]), "Selective subscription dispatched an unrelated event or skipped a selected one.");
            passed.Add("sparse pages register every position and dispatch selected types only");
            Console.WriteLine(JsonSerializer.Serialize(new { pullSelfTest = "PASS", cases = passed.Count, passed }));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { pullSelfTest = "FAIL", passed, error = error.ToString() }));
            return 1;
        }
    }
}
