// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines the system that manages the queues for appended events.
/// </summary>
public interface IAppendedEventsQueues : IGrainWithStringKey
{
    /// <summary>
    /// Enqueue an appended event.
    /// </summary>
    /// <param name="appendedEvents">Collection of <see cref="AppendedEvent"/> to enqueue.</param>
    /// <returns>Awaitable task.</returns>
    Task Enqueue(IEnumerable<AppendedEvent> appendedEvents);

    /// <summary>
    /// Subscribe an observer to the queue.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> for the subscriber to subscribe.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to subscribe to.</param>
    /// <param name="filters">Optional <see cref="ObserverFilters"/> to apply when dispatching events.</param>
    /// <returns><see cref="AppendedEventsQueueSubscription"/>.</returns>
    Task<AppendedEventsQueueSubscription> Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes, ObserverFilters? filters = null);

    /// <summary>
    /// Unsubscribe an observer from the queue.
    /// </summary>
    /// <param name="subscription"><see cref="AppendedEventsQueueSubscription"/> to unsubscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(AppendedEventsQueueSubscription subscription);

    /// <summary>
    /// Recover every subscribed observer across all queues through their catch-up path, dropping their live
    /// subscriptions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Used by the append side when a batch of durably appended events could not be handed to the queues at all, so
    /// nothing would otherwise drive the affected observers past the gap. Coarse by design - it spills every observer
    /// on the sequence - but safe, because an extra catch-up is idempotent while a lost live delivery is not.
    /// </remarks>
    Task SpillToCatchup();

    /// <summary>
    /// Check whether an observer is currently subscribed on the queue it routes to.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> of the observer to check.</param>
    /// <returns>True if the queue still holds a live subscription for the observer, false if not.</returns>
    /// <remarks>
    /// The authoritative answer comes from the queue grain itself, not from the routing index, because the queue can
    /// drop subscriptions behind an observer's back - it does so when it spills to catch-up under back-pressure. An
    /// observer that believes it is subscribed uses this to tell "still being delivered to" from "silently dropped".
    /// </remarks>
    Task<bool> IsSubscribed(ObserverKey observerKey);
}
