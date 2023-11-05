// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an observer in the system.
/// </summary>
public interface IObserver : IStateMachine<ObserverState>, IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Set metadata associated with the observer.
    /// </summary>
    /// <param name="name">Friendly name of the observer.</param>
    /// <param name="type"><see cref="ObserverType"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task SetNameAndType(ObserverName name, ObserverType type);

    /// <summary>
    /// Get the subscription for the observer.
    /// </summary>
    /// <returns>Tbe <see cref="ObserverSubscription"/>.</returns>
    Task<ObserverSubscription> GetSubscription();

    /// <summary>
    /// Check if the observer has a subscription subscribed.
    /// </summary>
    /// <returns>True if it has, false if not.</returns>
    Task<bool> IsSubscribed();

    /// <summary>
    /// Get the event types that the observer is observing.
    /// </summary>
    /// <returns>Collection of <see cref="EventType"/>.</returns>
    Task<IEnumerable<EventType>> GetEventTypes();

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
    Task ReplayPartition(Key partition);

    /// <summary>
    /// Rewind the observer for a specific partition to a specific sequence number.
    /// </summary>
    /// <param name="partition">The partition to rewind.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to rewind to.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Notify that the partition has been replayed.
    /// </summary>
    /// <param name="partition">The partition that has been replayed.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionReplayed(Key partition);

    /// <summary>
    /// Notify that the partition has failed.
    /// </summary>
    /// <param name="partition">The partition that failed.</param>
    /// <param name="sequenceNumber">The sequence number of the failure.</param>
    /// <param name="exceptionMessages">All exception messages.</param>
    /// <param name="exceptionStackTrace">The exception stacktrace.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionFailed(Key partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace);

    /// <summary>
    /// Notify that the partition has recovered.
    /// </summary>
    /// <param name="partition">The partition that has recovered.</param>
    /// <returns>Awaitable task.</returns>
    Task FailedPartitionRecovered(Key partition);

    /// <summary>
    /// Attempt to recover a failed partition.
    /// </summary>
    /// <param name="partition">The partition that is failed.</param>
    /// <returns>Awaitable task.</returns>
    Task TryRecoverFailedPartition(Key partition);

    /// <summary>
    /// Attempt to recover all failed partitions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task TryRecoverAllFailedPartitions();

    /// <summary>
    /// Handle events for an <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to handle events for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(Key partition, IEnumerable<AppendedEvent> events);

    /// <summary>
    /// Report a count of events that has been handled.
    /// </summary>
    /// <param name="count"><see cref="EventCount"/> to increase the handled count with.</param>
    /// <returns>Awaitable task.</returns>
    Task ReportHandledEvents(EventCount count);
}
