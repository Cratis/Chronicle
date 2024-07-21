// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.EventSequences;

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
    /// <returns><see cref="AppendedEventsQueueSubscription"/>.</returns>
    Task<AppendedEventsQueueSubscription> Subscribe(ObserverKey observerKey);

    /// <summary>
    /// Unsubscribe an observer from the queue.
    /// </summary>
    /// <param name="subscription"><see cref="AppendedEventsQueueSubscription"/> to unsubscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(AppendedEventsQueueSubscription subscription);
}
