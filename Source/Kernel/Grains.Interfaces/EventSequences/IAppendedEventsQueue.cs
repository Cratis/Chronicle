// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Defines a queue for appended events.
/// </summary>
public interface IAppendedEventsQueue : IGrainWithIntegerCompoundKey
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
    /// <returns>Awaitable task.</returns>
    Task Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Unsubscribe an observer from the queue.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> for the subscriber to unsubscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(ObserverKey observerKey);
}
