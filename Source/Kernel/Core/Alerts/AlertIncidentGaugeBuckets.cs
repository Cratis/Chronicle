// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Folds successive grouped counts into the series to publish, keeping emptied series at zero for a bounded period.
/// </summary>
/// <param name="Open">Buckets with at least one open incident.</param>
/// <param name="RetireAt">Emptied buckets and when they stop reporting zero.</param>
public sealed record AlertIncidentGaugeBuckets(
    IReadOnlyDictionary<AlertIncidentGaugeBucket, long> Open,
    IReadOnlyDictionary<AlertIncidentGaugeBucket, DateTimeOffset> RetireAt)
{
    /// <summary>
    /// The state before any count was folded.
    /// </summary>
    public static readonly AlertIncidentGaugeBuckets Empty = new(new Dictionary<AlertIncidentGaugeBucket, long>(), new Dictionary<AlertIncidentGaugeBucket, DateTimeOffset>());

    /// <summary>
    /// Gets the values to publish: open counts and zero for retiring buckets.
    /// </summary>
    public IEnumerable<(AlertIncidentGaugeBucket Bucket, long Value)> Values =>
        Open.Select(_ => (_.Key, _.Value)).Concat(RetireAt.Select(_ => (_.Key, 0L)));

    /// <summary>
    /// Folds the next grouped count.
    /// </summary>
    /// <param name="counts">The complete grouped open count.</param>
    /// <param name="now">The current time.</param>
    /// <param name="zeroRetention">How long an emptied bucket reports zero.</param>
    /// <returns>The next state.</returns>
    public AlertIncidentGaugeBuckets Next(IEnumerable<AlertIncidentObserverCount> counts, DateTimeOffset now, TimeSpan zeroRetention)
    {
        var open = new Dictionary<AlertIncidentGaugeBucket, long>();
        foreach (var count in counts.Where(_ => _.Count > 0))
        {
            var bucket = new AlertIncidentGaugeBucket(count.EventStore, count.Namespace, count.ObserverId, count.EventSequenceId, count.Condition, count.Severity);
            open[bucket] = open.GetValueOrDefault(bucket) + count.Count;
        }

        var retireAt = new Dictionary<AlertIncidentGaugeBucket, DateTimeOffset>();
        foreach (var (bucket, at) in RetireAt)
        {
            if (at > now && !open.ContainsKey(bucket))
            {
                retireAt[bucket] = at;
            }
        }
        foreach (var bucket in Open.Keys.Where(_ => !open.ContainsKey(_)))
        {
            retireAt[bucket] = now + zeroRetention;
        }

        return new(open, retireAt);
    }
}
