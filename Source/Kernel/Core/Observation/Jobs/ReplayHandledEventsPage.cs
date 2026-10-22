// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Collects handled-event reports for a replay page, merging each partition's counts and retaining its latest position.
/// </summary>
internal sealed class ReplayHandledEventsPage
{
    readonly Dictionary<Key, HandledEventsReport> _reports = [];

    /// <summary>
    /// Gets the aggregated reports for the partitions handled in the page.
    /// </summary>
    public IReadOnlyList<HandledEventsReport> Reports => _reports.Values.ToArray();

    /// <summary>
    /// Gets the highest event sequence number handled in the page.
    /// </summary>
    public EventSequenceNumber LastHandled { get; private set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Adds a handled run to the page's per-partition totals.
    /// </summary>
    /// <param name="report">The handled run to aggregate.</param>
    public void Add(HandledEventsReport report)
    {
        if (!report.HandledUpTo.IsActualValue) return;
        if (_reports.TryGetValue(report.Partition, out var previous))
        {
            var counts = previous.Counts.ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var (type, count) in report.Counts)
            {
                counts[type] = counts.GetValueOrDefault(type, EventCount.Zero) + count.Value;
            }

            report = report with { Counts = counts, HandledUpTo = previous.HandledUpTo > report.HandledUpTo ? previous.HandledUpTo : report.HandledUpTo };
        }

        _reports[report.Partition] = report;
        if (!LastHandled.IsActualValue || report.HandledUpTo > LastHandled) LastHandled = report.HandledUpTo;
    }

    /// <summary>
    /// Clears the reports and position after the page has been reported.
    /// </summary>
    public void Clear()
    {
        _reports.Clear();
        LastHandled = EventSequenceNumber.Unavailable;
    }
}
