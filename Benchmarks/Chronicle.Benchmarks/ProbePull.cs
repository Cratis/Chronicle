// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Channels;

namespace Cratis.Chronicle.Benchmarks;

internal sealed record ProbePullOptions(int Lanes, int Page, int FlushMs, string Stats, string Wake, ulong? DropSequence)
{
    internal static ProbePullOptions FromEnvironment()
    {
        var result = new ProbePullOptions(int.Parse(Environment.GetEnvironmentVariable("PULL_LANES") ?? "64"),
            int.Parse(Environment.GetEnvironmentVariable("PULL_PAGE") ?? "1000"), int.Parse(Environment.GetEnvironmentVariable("PULL_FLUSH_MS") ?? "50"),
            Environment.GetEnvironmentVariable("PULL_STATS") ?? "equivalent", Environment.GetEnvironmentVariable("PULL_WAKE") ?? "hint",
            ulong.TryParse(Environment.GetEnvironmentVariable("PULL_DROP_SEQ"), out var drop) ? drop : null);
        if (result.Lanes < 1 || result.Page < 1 || result.FlushMs < 1 || result.Stats is not ("checkpoint" or "equivalent") || result.Wake is not ("hint" or "poll"))
            throw new ArgumentException("Invalid pull options.");
        return result;
    }
}

internal interface IPullPageSource
{
    Task<PullEvent[]> Read(PullCursor? cursor, int page, CancellationToken cancellation);
}

internal interface IPullProgress
{
    Task Save(PullPrefix prefix, IReadOnlyDictionary<string, long> counts, CancellationToken cancellation);
}

internal sealed class ProbePullCoordinator : IAsyncDisposable
{
    readonly IPullPageSource _source;
    readonly IPullProgress _progress;
    readonly Func<PullEvent, Task> _handle;
    readonly ProbePullOptions _options;
    readonly ProbePullLedger _ledger;
    readonly Channel<ProbePullLedger.Entry>[] _lanes;
    readonly SemaphoreSlim _wake = new(0, 1);
    readonly CancellationTokenSource _stop;
    readonly Task[] _tasks;
    long _gapHolds;
    double _maxGapMs;
    long _reads;
    PullCursor? _cursor;

    internal ProbePullCoordinator(IPullPageSource source, IPullProgress progress, ProbePullOptions options,
        Func<PullEvent, Task> handle, CancellationToken cancellation)
    {
        _source = source;
        _progress = progress;
        _options = options;
        _handle = handle;
        _ledger = new(Math.Max(65536, checked(options.Page * 2)));
        _lanes = Enumerable.Range(0, options.Lanes).Select(_ => Channel.CreateBounded<ProbePullLedger.Entry>(new BoundedChannelOptions(1024)
        { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait })).ToArray();
        _stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        _tasks = _lanes.Select(Consume).Append(Task.Run(Read)).Append(Task.Run(Flush)).ToArray();
    }

    internal long Reads => Interlocked.Read(ref _reads);
    internal long GapHolds => Interlocked.Read(ref _gapHolds);
    internal double MaxGapHoldMs => _maxGapMs;
    internal void Wake()
    {
        if (_options.Wake == "hint" && _wake.CurrentCount == 0)
        {
            try { _wake.Release(); }
            catch (SemaphoreFullException) { /* Coalesced append-ack hint. */ }
        }
    }

    internal void CheckHealth()
    {
        foreach (var task in _tasks) if (task.IsFaulted) task.GetAwaiter().GetResult();
    }

    async Task Read()
    {
        long? gapStarted = null;
        while (!_stop.IsCancellationRequested)
        {
            Interlocked.Increment(ref _reads);
            var page = await _source.Read(_cursor, _options.Page, _stop.Token);
            var entries = new List<ProbePullLedger.Entry>();
            var gap = false;
            foreach (var evt in page)
            {
                var next = _cursor is null ? 0 : checked(_cursor.Position + 1);
                if (evt.Sequence != next) { gap = true; break; }
                entries.Add(await _ledger.Register(evt, _stop.Token));
                _cursor = new(evt.Sequence);
            }
            // All entries in this page's contiguous prefix exist before any handler can finish.
            foreach (var entry in entries)
            {
                if (entry.Event.Payload is null || entry.Event.Sequence == _options.DropSequence)
                    Volatile.Write(ref entry.Done, 1);
                else await _lanes[Lane(entry.Event.Key, _lanes.Length)].Writer.WriteAsync(entry, _stop.Token);
            }
            if (gap)
            {
                Interlocked.Increment(ref _gapHolds);
                gapStarted ??= Stopwatch.GetTimestamp();
                _maxGapMs = Math.Max(_maxGapMs, Stopwatch.GetElapsedTime(gapStarted.Value).TotalMilliseconds);
                if (_maxGapMs > 2000) throw new InvalidOperationException("Pull visibility gap held for over 2 seconds; no gap crossed.");
                await Task.Delay(5, _stop.Token);
            }
            else if (page.Length == 0)
            {
                // A known gap remains known even if a later reread is empty.
                if (gapStarted.HasValue && Stopwatch.GetElapsedTime(gapStarted.Value).TotalMilliseconds > 2000)
                    throw new InvalidOperationException("Pull visibility gap did not close within 2 seconds.");
                await _wake.WaitAsync(TimeSpan.FromMilliseconds(100), _stop.Token);
            }
            else gapStarted = null;
        }
    }

    async Task Consume(Channel<ProbePullLedger.Entry> lane)
    {
        await foreach (var entry in lane.Reader.ReadAllAsync(_stop.Token))
        {
            await _handle(entry.Event);
            Volatile.Write(ref entry.Done, 1);
        }
    }

    async Task Flush()
    {
        ulong? saved = null;
        long? firstUnflushed = null;
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        while (!_stop.IsCancellationRequested)
        {
            var prefix = _ledger.Advance();
            foreach (var (key, count) in prefix.PerKey) counts[key] = counts.GetValueOrDefault(key) + count;
            if (prefix.W != saved)
            {
                firstUnflushed ??= Stopwatch.GetTimestamp();
                var completions = prefix.W!.Value - (saved.HasValue ? saved.Value : 0) + (saved.HasValue ? 0UL : 1UL);
                if (completions >= 1000 || Stopwatch.GetElapsedTime(firstUnflushed.Value).TotalMilliseconds >= _options.FlushMs)
                {
                    await _progress.Save(prefix, counts, _stop.Token);
                    saved = prefix.W;
                    counts.Clear();
                    firstUnflushed = null;
                }
            }
            await Task.Delay(1, _stop.Token);
        }
    }

    internal static int Lane(string key, int lanes)
    {
        uint hash = 2166136261;
        foreach (var c in key) hash = unchecked((hash ^ c) * 16777619);
        return (int)(hash % (uint)lanes);
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        try { await Task.WhenAll(_tasks); }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { CheckHealth(); }
        _stop.Dispose();
        _wake.Dispose();
        // No shutdown write: the measured durable tail must already have been saved by the normal cadence.
    }
}
