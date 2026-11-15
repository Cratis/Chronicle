// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.CodeGeneration;
using Orleans.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines a queue for appended events.
/// </summary>
[Version(1)]
public interface IAppendedEventsQueue : IGrainWithIntegerCompoundKey
{
    /// <summary>
    /// Enqueue an appended event.
    /// </summary>
    /// <param name="appendedEvents">Collection of <see cref="AppendedEvent"/> to enqueue.</param>
    /// <returns>Awaitable task.</returns>
    Task Enqueue(IEnumerable<AppendedEvent> appendedEvents);

    /// <summary>
    /// Discards live subscriptions and queued events and waits for outstanding delivery to stop before a development or integration storage wipe.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>Development reset only; the activation must be deactivated afterwards, not reused for production delivery.</remarks>
    [AlwaysInterleave]
    Task ResetForDevelopment();

    /// <summary>
    /// Subscribe an observer to the queue.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> for the subscriber to subscribe.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to subscribe to.</param>
    /// <param name="filters">Optional <see cref="ObserverFilters"/> to apply when dispatching events.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes, ObserverFilters? filters = null);

    /// <summary>
    /// Subscribe an observer to every event appended to the queue, whatever its type - including event types that do
    /// not exist yet.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> for the subscriber to subscribe.</param>
    /// <param name="filters">Optional <see cref="ObserverFilters"/> to apply when dispatching events.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeToAllEventTypes(ObserverKey observerKey, ObserverFilters? filters = null);

    /// <summary>
    /// Unsubscribe an observer from the queue.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> for the subscriber to unsubscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(ObserverKey observerKey);

    /// <summary>
    /// Recover the queue's subscribed observers through their catch-up path, dropping their live subscriptions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The queue does this on its own when back-pressure makes it skip a batch. It is exposed so the same recovery
    /// can be driven from the append side when a batch never reached the queue at all - the events are durable
    /// either way, and catch-up re-reads them from each observer's persisted cursor.
    /// </remarks>
    Task SpillToCatchup();

    /// <summary>
    /// Get a snapshot of the current subscriptions on the queue.
    /// </summary>
    /// <returns>Collection of <see cref="AppendedEventsQueueObserverSubscription"/> currently subscribed.</returns>
    /// <remarks>
    /// Used by <see cref="IAppendedEventsQueues"/> to reconcile its routing index on activation, since a queue's
    /// subscriptions can outlive the routing grain's in-memory state.
    /// </remarks>
    Task<IReadOnlyList<AppendedEventsQueueObserverSubscription>> GetSubscriptions();

    /// <summary>
    /// Hands every subscribed observer the given events are meant for to its catch-up path, dropping its live
    /// subscription, without delivering the events live.
    /// </summary>
    /// <param name="appendedEvents">The durable events that were committed without live delivery.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Used by an import commit (decision 0023), after each chunk is durable: the chunk is not delivered live, so an
    /// ordinary append delivered after it would carry an observer's cursor past imported events it has not seen. This is
    /// the spill the queue applies to a lagging observer under back-pressure; an observer that is not subscribed, because
    /// it is already catching up, is left alone and gets no second catch-up. Catch-up reads from the observer's own
    /// cursor, so it delivers the imported events and any ordinary appends that interleave with them, in order.
    /// </remarks>
    Task SpillMatchingToCatchup(IEnumerable<AppendedEvent> appendedEvents);
}
