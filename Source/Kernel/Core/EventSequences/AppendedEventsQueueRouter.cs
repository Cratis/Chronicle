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
    /// Observers subscribed to every event type have no set of types to fold into the union - a queue holding one of
    /// them has to receive every batch, so they are tracked apart from the typed subscriptions.
    /// </summary>
    readonly Dictionary<int, HashSet<ObserverKey>> _allEventTypesByQueue = [];

    /// <summary>
    /// Gets the deterministic queue index an observer is assigned to.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> to resolve the queue for.</param>
    /// <returns>The zero-based queue index.</returns>
    /// <remarks>
    /// The assignment is a <see cref="StableHash"/> of the observer identifier, so the same observer always lands on
    /// the same queue - across subscribe/unsubscribe cycles, across activations of this grain, and across silos.
    /// <see cref="string.GetHashCode()"/> would not do: it is seeded randomly per process, so the assignment would
    /// change whenever the grain reactivated in a new process, leaving an observer's earlier subscription stranded on
    /// the queue it was originally assigned to and receiving events twice.
    /// </remarks>
    public int GetQueueIndexFor(ObserverKey observerKey) =>
        (int)(StableHash.Of(observerKey.ObserverId.Value) % (uint)queueCount);

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
        var allEventTypes = new HashSet<ObserverKey>();
        foreach (var subscription in subscriptions)
        {
            if (subscription.AllEventTypes)
            {
                allEventTypes.Add(subscription.ObserverKey);
                continue;
            }

            byObserver[subscription.ObserverKey] = subscription.EventTypeIds.ToHashSet();
        }

        _subscriptionsByQueue[queueIndex] = byObserver;
        _allEventTypesByQueue[queueIndex] = allEventTypes;
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

        if (_allEventTypesByQueue.TryGetValue(queueIndex, out var allEventTypes))
        {
            allEventTypes.Remove(observerKey);
        }

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
    /// Records that an observer subscribed to every event type, including event types that do not exist yet.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> of the subscribing observer.</param>
    /// <returns>The queue index the observer was assigned to.</returns>
    public int SubscribeToAllEventTypes(ObserverKey observerKey)
    {
        var queueIndex = GetQueueIndexFor(observerKey);

        // A re-subscription replaces the previous one, so any typed subscription the observer held goes.
        if (_subscriptionsByQueue.TryGetValue(queueIndex, out var byObserver) && byObserver.Remove(observerKey))
        {
            _unionByQueue[queueIndex] = ComputeUnion(byObserver);
        }

        if (!_allEventTypesByQueue.TryGetValue(queueIndex, out var allEventTypes))
        {
            allEventTypes = [];
            _allEventTypesByQueue[queueIndex] = allEventTypes;
        }

        allEventTypes.Add(observerKey);
        return queueIndex;
    }

    /// <summary>
    /// Removes an observer's subscription from a queue.
    /// </summary>
    /// <param name="queueIndex">Index of the queue the observer was subscribed to.</param>
    /// <param name="observerKey"><see cref="ObserverKey"/> of the unsubscribing observer.</param>
    public void Unsubscribe(int queueIndex, ObserverKey observerKey)
    {
        if (_allEventTypesByQueue.TryGetValue(queueIndex, out var allEventTypes))
        {
            allEventTypes.Remove(observerKey);
        }

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

            if (_allEventTypesByQueue.TryGetValue(queueIndex, out var allEventTypes) && allEventTypes.Count > 0)
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
