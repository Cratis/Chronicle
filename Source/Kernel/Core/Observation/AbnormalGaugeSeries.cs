// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Decides which series of a per-observer gauge are reported: only those whose value is above zero, followed by a
/// time-limited run of zeros once the value returns to zero.
/// </summary>
/// <typeparam name="TKey">The type identifying a series.</typeparam>
/// <param name="clock">The <see cref="TimeProvider"/> measuring the zero tail.</param>
/// <param name="zeroTail">How long a series keeps reporting zero after it was last seen above zero.</param>
/// <remarks>
/// A healthy observer therefore costs no series at all. The zero tail lets a backend see the recovery rather than
/// holding the last abnormal value, which it would do because the OpenTelemetry SDK stops exporting a series that a
/// callback no longer reports. It also covers an observer that was released while abnormal. The tail is measured from
/// the last collection that saw the series above zero, and a series is dropped only once the tail has passed and at
/// least one collection has reported its zero, so a collection interval as long as the tail still sees one zero. With
/// several readers, for example an OTLP exporter and a Prometheus endpoint, that zero is guaranteed only to the first
/// reader collecting after the recovery; a reader collecting less often than the tail can miss it.
/// </remarks>
internal sealed class AbnormalGaugeSeries<TKey>(TimeProvider clock, TimeSpan zeroTail)
    where TKey : notnull
{
    readonly ConcurrentDictionary<TKey, SeenAbnormal> _seen = new();

    /// <summary>
    /// Produces the measurements for one collection.
    /// </summary>
    /// <param name="current">The current value and attributes of every series that has one.</param>
    /// <returns>The measurements to report.</returns>
    internal Measurement<int>[] Observe(IEnumerable<(TKey Key, int Value, KeyValuePair<string, object?>[] Tags)> current)
    {
        var now = clock.GetUtcNow();
        var measurements = new List<Measurement<int>>();
        var reported = new HashSet<TKey>();
        foreach (var (key, value, tags) in current)
        {
            if (value <= 0) continue;
            _seen[key] = new(tags, now, ZeroReported: false);
            reported.Add(key);
            measurements.Add(new(value, tags));
        }

        foreach (var seen in _seen)
        {
            if (reported.Contains(seen.Key)) continue;
            if (seen.Value.ZeroReported && now - seen.Value.At >= zeroTail)
            {
                _seen.TryRemove(seen);
                continue;
            }

            if (!seen.Value.ZeroReported)
            {
                _seen.TryUpdate(seen.Key, seen.Value with { ZeroReported = true }, seen.Value);
            }

            measurements.Add(new(0, seen.Value.Tags));
        }

        return [.. measurements];
    }

    sealed record SeenAbnormal(KeyValuePair<string, object?>[] Tags, DateTimeOffset At, bool ZeroReported);
}
