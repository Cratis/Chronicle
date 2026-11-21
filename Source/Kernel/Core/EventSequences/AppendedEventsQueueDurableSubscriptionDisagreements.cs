// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The observers whose stored durable subscription and live queue subscription disagree (decision 0034, shadow stage).
/// </summary>
/// <param name="StoredNotLive">Observers with a stored durable subscription and no live subscription in the queue.</param>
/// <param name="LiveNotStored">Observers with a live subscription in the queue and no stored durable subscription.</param>
public record AppendedEventsQueueDurableSubscriptionDisagreements(
    IReadOnlyList<ObserverId> StoredNotLive,
    IReadOnlyList<ObserverId> LiveNotStored)
{
    /// <summary>
    /// Gets the metric attribute value for <see cref="StoredNotLive"/>.
    /// </summary>
    public const string StoredNotLiveKind = "stored_not_live";

    /// <summary>
    /// Gets the metric attribute value for <see cref="LiveNotStored"/>.
    /// </summary>
    public const string LiveNotStoredKind = "live_not_stored";

    /// <summary>
    /// Gets whether the stored and live subscriptions agree.
    /// </summary>
    public bool Agree => StoredNotLive.Count == 0 && LiveNotStored.Count == 0;

    /// <summary>
    /// Compares the stored durable subscriptions of a queue with its live subscriptions.
    /// </summary>
    /// <param name="stored">The observers that have a stored durable subscription and belong to the queue.</param>
    /// <param name="live">The observers that hold a live subscription in the queue.</param>
    /// <returns>The disagreements, ordered by observer identifier so a report is stable.</returns>
    public static AppendedEventsQueueDurableSubscriptionDisagreements Compare(IEnumerable<ObserverId> stored, IEnumerable<ObserverId> live)
    {
        var storedSet = stored.ToHashSet();
        var liveSet = live.ToHashSet();
        return new(
            [.. storedSet.Where(observer => !liveSet.Contains(observer)).OrderBy(observer => observer.Value, StringComparer.Ordinal)],
            [.. liveSet.Where(observer => !storedSet.Contains(observer)).OrderBy(observer => observer.Value, StringComparer.Ordinal)]);
    }
}
