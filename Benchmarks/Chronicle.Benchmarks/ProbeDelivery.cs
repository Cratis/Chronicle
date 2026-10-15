// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Benchmarks;

internal sealed record ProbePosition(string Key, ulong SequenceNumber);

internal sealed class ProbeDelivery
{
    readonly object _gate = new();
    readonly Dictionary<ProbePosition, (long First, int Count)> _delivered = [];
    long _busyTicks;

    internal static ProbeDelivery Current { get; set; } = new();

    internal bool Drop { get; init; }

    internal int DropIndex { get; init; } = 1;

    internal double BusyMs => Interlocked.Read(ref _busyTicks) * 1000.0 / Stopwatch.Frequency;

    internal void Record(ProbePosition position, int payloadIndex)
    {
        var start = Stopwatch.GetTimestamp();
        if (!Drop || payloadIndex != DropIndex)
        {
            lock (_gate)
            {
                if (_delivered.TryGetValue(position, out var prior))
                {
                    _delivered[position] = (prior.First, prior.Count + 1);
                }
                else
                {
                    _delivered[position] = (Stopwatch.GetTimestamp(), 1);
                }
            }
        }
        Interlocked.Add(ref _busyTicks, Stopwatch.GetTimestamp() - start);
    }

    internal object[] FirstDeliveries(long started)
    {
        lock (_gate)
        {
            var now = Stopwatch.GetTimestamp();
            var utcNow = DateTimeOffset.UtcNow;
            return _delivered.OrderBy(p => p.Value.First).Select(p => (object)new
            {
                key = p.Key.Key,
                sequenceNumber = p.Key.SequenceNumber,
                count = p.Value.Count,
                msFromStart = (p.Value.First - started) * 1000.0 / Stopwatch.Frequency,
                utc = utcNow.AddTicks(-(long)((now - p.Value.First) * (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency))
            }).ToArray();
        }
    }

    internal ProbeAccounting Snapshot(IReadOnlyCollection<ProbePosition> expected, long started)
    {
        lock (_gate)
        {
            var membership = expected.ToHashSet();
            var missing = membership.Where(p => !_delivered.ContainsKey(p)).OrderBy(p => p.SequenceNumber).ToArray();
            var matching = _delivered.Where(p => membership.Contains(p.Key)).ToArray();
            var violations = matching.GroupBy(p => p.Key.Key).Sum(group =>
            {
                ulong? previous = null;
                var count = 0;
                foreach (var item in group.OrderBy(p => p.Value.First))
                {
                    if (previous.HasValue && item.Key.SequenceNumber < previous.Value) count++;
                    previous = previous.HasValue ? Math.Max(previous.Value, item.Key.SequenceNumber) : item.Key.SequenceNumber;
                }
                return count;
            });
            double? effectMs = missing.Length == 0 && membership.Count > 0
                ? (matching.Max(p => p.Value.First) - started) * 1000.0 / Stopwatch.Frequency
                : null;
            return new(membership.Count, matching.Length, _delivered.Sum(p => p.Value.Count - 1), violations,
                missing, _delivered.Keys.Where(p => !membership.Contains(p)).ToArray(), effectMs);
        }
    }
}

internal sealed record ProbeAccounting(
    int Expected,
    int DistinctDelivered,
    int Duplicates,
    int FirstDeliveryOrderViolations,
    ProbePosition[] Missing,
    ProbePosition[] Unexpected,
    double? EffectMs);
