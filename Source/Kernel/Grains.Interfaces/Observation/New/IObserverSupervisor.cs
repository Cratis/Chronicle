// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.New;

/// <summary>
/// Represents a supervisor for <see cref="IObserver"/>.
/// </summary>
public interface IObserverSupervisor : IStateMachine<ObserverState>
{
    /// <summary>
    /// Set metadata associated with the observer.
    /// </summary>
    /// <param name="name">Friendly name of the observer.</param>
    /// <param name="type"><see cref="ObserverType"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task SetNameAndType(ObserverName name, ObserverType type);

    /// <summary>
    /// Subscribe to observer.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">Type of <see cref="IObserverSubscriber"/> to subscribe.</typeparam>
    /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> to subscribe to.</param>
    /// <param name="subscriberArgs">Optional arguments associated with the subscription.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe<TObserverSubscriber>(IEnumerable<EventType> eventTypes, object? subscriberArgs = default)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Unsubscribe from the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();

    /// <summary>
    /// Rewind the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Replay();

    /// <summary>
    /// Rewind the observer for a specific partition.
    /// </summary>
    /// <param name="partition">The partition to rewind.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartition(EventSourceId partition);

    /// <summary>
    /// Rewind the observer for a specific partition to a specific sequence number.
    /// </summary>
    /// <param name="partition">The partition to rewind.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to rewind to.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartitionTo(EventSourceId partition, EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Notify that the partition has failed.
    /// </summary>
    /// <param name="partition">The partition that failed.</param>
    /// <param name="sequenceNumber">The sequence number of the failure.</param>
    /// <param name="exceptionMessages">All exception messages.</param>
    /// <param name="exceptionStackTrace">The exception stacktrace.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace);

    /// <summary>
    /// Handle events for an <see cref="EventSourceId/">.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to handle events for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(EventSourceId eventSourceId, IEnumerable<AppendedEvent> events);
}
