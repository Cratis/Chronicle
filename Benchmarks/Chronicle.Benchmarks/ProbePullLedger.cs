// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks;

internal sealed record PullEvent(ulong Sequence, string Key, string Type, ProbePayload? Payload);
internal sealed record PullCursor(ulong Position);
internal sealed record PullPrefix(ulong? W, ulong? R, long Handled, Dictionary<string, long> PerKey);

// One ordered, bounded ledger. Registration precedes publication and dispatch; only the flusher removes entries.
internal sealed class ProbePullLedger(int capacity)
{
    internal sealed class Entry(PullEvent evt)
    {
        internal readonly PullEvent Event = evt;
        internal int Done;
    }

    readonly object _gate = new();
    readonly Queue<Entry> _entries = new();
    readonly SemaphoreSlim _space = new(capacity, capacity);
    ulong? _r;
    ulong? _w;
    long _handled;

    internal async Task<Entry> Register(PullEvent evt, CancellationToken cancellation)
    {
        await _space.WaitAsync(cancellation);
        lock (_gate)
        {
            var next = _r.HasValue ? checked(_r.Value + 1) : 0;
            if (evt.Sequence != next) throw new InvalidOperationException($"Ledger gap: expected {next}, got {evt.Sequence}.");
            var entry = new Entry(evt);
            _entries.Enqueue(entry);
            _r = evt.Sequence;
            return entry;
        }
    }

    internal PullPrefix Advance()
    {
        lock (_gate)
        {
            var counts = new Dictionary<string, long>(StringComparer.Ordinal);
            while (_entries.TryPeek(out var entry) && Volatile.Read(ref entry.Done) == 1)
            {
                _entries.Dequeue();
                _w = entry.Event.Sequence;
                if (entry.Event.Payload is not null)
                {
                    _handled++;
                    counts[entry.Event.Key] = counts.GetValueOrDefault(entry.Event.Key) + 1;
                }
                _space.Release();
            }
            return new(_w, _r, _handled, counts);
        }
    }
}
