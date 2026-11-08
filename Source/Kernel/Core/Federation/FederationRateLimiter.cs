// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Limits how many events and content bytes each federation link may be served per second, with one token bucket pair per link.
/// </summary>
/// <remarks>
/// Buckets are independent, so a link that exhausts its budget never affects another. The only synchronization is a short
/// lock on the bucket of the one link, held for arithmetic and never across I/O. A pull is admitted while both buckets are
/// positive and is charged what it actually delivered afterwards, so a single large batch drives the balance negative and
/// the link is throttled until it has paid the debt back.
/// </remarks>
/// <param name="timeProvider">The <see cref="TimeProvider"/> used for refilling.</param>
[Singleton]
public class FederationRateLimiter(TimeProvider timeProvider)
{
    readonly ConcurrentDictionary<string, Bucket> _buckets = new(StringComparer.Ordinal);

    /// <summary>
    /// Check whether a link may start a pull now.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <param name="maxEventsPerSecond">The events per second the link may be served.</param>
    /// <param name="maxBytesPerSecond">The content bytes per second the link may be served.</param>
    /// <returns>True when the link has budget left, false when it must be throttled.</returns>
    public bool TryBegin(string linkId, int maxEventsPerSecond, long maxBytesPerSecond)
    {
        var bucket = _buckets.GetOrAdd(
            linkId,
            static (_, state) => new Bucket(state.Timestamp, state.Events, state.Bytes),
            (Timestamp: timeProvider.GetTimestamp(), Events: maxEventsPerSecond, Bytes: maxBytesPerSecond));
        lock (bucket)
        {
            Refill(bucket, maxEventsPerSecond, maxBytesPerSecond);
            return bucket.Events > 0 && bucket.Bytes > 0;
        }
    }

    /// <summary>
    /// Charge a link for what a pull delivered.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <param name="events">The number of events delivered.</param>
    /// <param name="bytes">The number of content bytes delivered.</param>
    public void Consume(string linkId, int events, long bytes)
    {
        if (!_buckets.TryGetValue(linkId, out var bucket))
        {
            return;
        }

        lock (bucket)
        {
            bucket.Events -= events;
            bucket.Bytes -= bytes;
        }
    }

    void Refill(Bucket bucket, int maxEventsPerSecond, long maxBytesPerSecond)
    {
        var now = timeProvider.GetTimestamp();
        var elapsed = timeProvider.GetElapsedTime(bucket.LastRefill, now).TotalSeconds;
        bucket.LastRefill = now;
        if (elapsed <= 0)
        {
            return;
        }

        // The capacity is one second of budget, so an idle link can burst by at most one second's worth.
        bucket.Events = Math.Min(maxEventsPerSecond, bucket.Events + (elapsed * maxEventsPerSecond));
        bucket.Bytes = Math.Min(maxBytesPerSecond, bucket.Bytes + (elapsed * maxBytesPerSecond));
    }

    sealed class Bucket(long lastRefill, double events, double bytes)
    {
        public long LastRefill { get; set; } = lastRefill;
        public double Events { get; set; } = events;
        public double Bytes { get; set; } = bytes;
    }
}
