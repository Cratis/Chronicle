// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Maintains, per appended-events queue, the union of event type identifiers its subscribed observers are
/// interested in, and decides which queues a batch of appended events must be delivered to.
/// </summary>
/// <param name="queueCount">Number of queues to route across.</param>
/// <remarks>
/// The router is an in-memory index owned by a single <see cref="AppendedEventsQueues"/> grain. It is not
/// thread-safe: it is mutated and read only within that non-reentrant grain's turn, the same way the grain
/// already guards its queue array. There is exactly one router per event sequence, so it needs no cluster-wide
/// coherency.
/// <para>
/// Routing is biased towards delivery. A queue is skipped for a batch only when the router holds the queue's
/// authoritative subscriptions (it has been <see cref="Seed"/>ed) and none of them match the batch. A queue whose
/// subscriptions are not yet known — before it is seeded, e.g. right after a grain reactivation where the queue
/// grains outlived this grain's in-memory state — is always delivered to. Redelivery is idempotent for observers,
/// so an extra delivery is harmless while a missed delivery would silently drop events forever.
/// </para>
/// </remarks>
public sealed class AppendedEventsQueueRouter(int queueCount)
{
    readonly Dictionary<int, Dictionary<ObserverKey, IReadOnlySet<EventTypeId>>> _subscriptionsByQueue = [];
    readonly Dictionary<int, HashSet<EventTypeId>> _unionByQueue = [];
    readonly HashSet<int> _seededQueues = [];

    /// <summary>
    /// Gets the deterministic queue index an observer is assigned to.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> to resolve the queue for.</param>
    /// <returns>The zero-based queue index.</returns>
    /// <remarks>
    /// The assignment is a stable hash of the observer identifier, so the same observer always lands on the
    /// same queue across subscribe/unsubscribe cycles.
    /// </remarks>
    public int GetQueueIndexFor(ObserverKey observerKey)
    {
        var hash = observerKey.ObserverId.Value.GetHashCode(StringComparison.Ordinal);
        return (int)((uint)hash % (uint)queueCount);
    }

    /// <summary>
    /// Seeds a queue with the authoritative snapshot of its current subscriptions.
    /// </summary>
    /// <param name="queueIndex">Index of the queue being seeded.</param>
    /// <param name="subscriptions">Current subscriptions on the queue.</param>
    /// <remarks>
    /// Called on activation to reconcile the in-memory index with the queue grains, whose subscription state can
    /// outlive an <see cref="AppendedEventsQueues"/> deactivation. Once seeded, the queue is trusted and may be
    /// skipped for batches none of its subscriptions match.
    /// </remarks>
    public void Seed(int queueIndex, IEnumerable<AppendedEventsQueueObserverSubscription> subscriptions)
    {
        var byObserver = new Dictionary<ObserverKey, IReadOnlySet<EventTypeId>>();
        foreach (var subscription in subscriptions)
        {
            byObserver[subscription.ObserverKey] = subscription.EventTypeIds.ToHashSet();
        }

        _subscriptionsByQueue[queueIndex] = byObserver;
        _unionByQueue[queueIndex] = ComputeUnion(byObserver);
        _seededQueues.Add(queueIndex);
    }

    /// <summary>
    /// Records that an observer subscribed to the event types it is interested in.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> of the subscribing observer.</param>
    /// <param name="eventTypeIds">Collection of <see cref="EventTypeId"/> the observer subscribes to.</param>
    /// <returns>The queue index the observer was assigned to.</returns>
    public int Subscribe(ObserverKey observerKey, IEnumerable<EventTypeId> eventTypeIds)
    {
        var queueIndex = GetQueueIndexFor(observerKey);
        var eventTypes = eventTypeIds.ToHashSet();

        if (!_subscriptionsByQueue.TryGetValue(queueIndex, out var byObserver))
        {
            byObserver = [];
            _subscriptionsByQueue[queueIndex] = byObserver;
        }

        byObserver[observerKey] = eventTypes;

        if (!_unionByQueue.TryGetValue(queueIndex, out var union))
        {
            union = [];
            _unionByQueue[queueIndex] = union;
        }

        union.UnionWith(eventTypes);
        return queueIndex;
    }

    /// <summary>
    /// Removes an observer's subscription from a queue.
    /// </summary>
    /// <param name="queueIndex">Index of the queue the observer was subscribed to.</param>
    /// <param name="observerKey"><see cref="ObserverKey"/> of the unsubscribing observer.</param>
    public void Unsubscribe(int queueIndex, ObserverKey observerKey)
    {
        if (!_subscriptionsByQueue.TryGetValue(queueIndex, out var byObserver) || !byObserver.Remove(observerKey))
        {
            return;
        }

        _unionByQueue[queueIndex] = ComputeUnion(byObserver);
    }

    /// <summary>
    /// Gets the indices of the queues a batch with the given event types must be delivered to.
    /// </summary>
    /// <param name="batchEventTypeIds">The distinct <see cref="EventTypeId"/> present in the batch.</param>
    /// <returns>The indices of the queues to deliver the batch to.</returns>
    public IReadOnlyList<int> GetQueuesToDeliverTo(IReadOnlyCollection<EventTypeId> batchEventTypeIds)
    {
        var queues = new List<int>(queueCount);
        for (var queueIndex = 0; queueIndex < queueCount; queueIndex++)
        {
            if (!_seededQueues.Contains(queueIndex))
            {
                queues.Add(queueIndex);
                continue;
            }

            if (_unionByQueue.TryGetValue(queueIndex, out var union) && batchEventTypeIds.Any(union.Contains))
            {
                queues.Add(queueIndex);
            }
        }

        return queues;
    }

    static HashSet<EventTypeId> ComputeUnion(Dictionary<ObserverKey, IReadOnlySet<EventTypeId>> byObserver)
    {
        var union = new HashSet<EventTypeId>();
        foreach (var eventTypes in byObserver.Values)
        {
            union.UnionWith(eventTypes);
        }

        return union;
    }
}
