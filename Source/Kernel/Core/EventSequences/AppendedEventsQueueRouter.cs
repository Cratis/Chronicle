// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.ObserverRouting;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Decides which appended-events queue an observer belongs to and which queues a batch of appended events must be
/// delivered to.
/// </summary>
/// <param name="queueCount">Number of queues to route across.</param>
/// <remarks>
/// The router holds no state. An observer's queue is a stable hash of its identifier, and the queues a batch goes to are
/// read from the silo-local <see cref="ObserverRoutingTable"/>, so nothing has to be seeded or kept alive for routing to
/// be right. Routing is biased towards delivery: without a table every queue receives the batch, because an extra
/// delivery is filtered out by the queue while a missed one would drop events.
/// </remarks>
public sealed class AppendedEventsQueueRouter(int queueCount)
{
    /// <summary>
    /// Gets the deterministic queue index an observer is assigned to.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> to resolve the queue for.</param>
    /// <returns>The zero-based queue index.</returns>
    /// <remarks>
    /// The assignment is a <see cref="StableHash"/> of the observer identifier, so the same observer always lands on
    /// the same queue - across subscribe/unsubscribe cycles, across activations, and across silos.
    /// <see cref="string.GetHashCode()"/> would not do: it is seeded randomly per process.
    /// </remarks>
    public int GetQueueIndexFor(ObserverKey observerKey) => GetQueueIndexFor(observerKey.ObserverId);

    /// <summary>
    /// Gets the deterministic queue index an observer is assigned to.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to resolve the queue for.</param>
    /// <returns>The zero-based queue index.</returns>
    public int GetQueueIndexFor(ObserverId observerId) =>
        (int)(StableHash.Of(observerId.Value) % (uint)queueCount);

    /// <summary>
    /// Gets the indices of the queues a batch with the given event types must be delivered to.
    /// </summary>
    /// <param name="table">The <see cref="ObserverRoutingTable"/> of the event store, or null when it could not be read.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the batch was appended to.</param>
    /// <param name="batchEventTypeIds">The distinct <see cref="EventTypeId"/> present in the batch.</param>
    /// <returns>The indices of the queues to deliver the batch to.</returns>
    public IReadOnlyList<int> GetQueuesToDeliverTo(ObserverRoutingTable? table, EventSequenceId eventSequenceId, IReadOnlyCollection<EventTypeId> batchEventTypeIds)
    {
        if (table is null)
        {
            return [.. Enumerable.Range(0, queueCount)];
        }

        return [.. batchEventTypeIds
            .SelectMany(eventTypeId => table.Match(eventSequenceId, eventTypeId))
            .Select(route => GetQueueIndexFor(route.ObserverId))
            .Distinct()
            .Order()];
    }
}
