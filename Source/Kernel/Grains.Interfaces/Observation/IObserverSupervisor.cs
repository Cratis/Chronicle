// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines an observer of an event sequence.
/// </summary>
public interface IObserverSupervisor : IGrainWithGuidCompoundKey
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
    /// Get the current subscription.
    /// </summary>
    /// <returns><see cref="ObserverSubscription"/>.</returns>
    Task<ObserverSubscription> GetCurrentSubscription();

    /// <summary>
    /// Unsubscribe from the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();

    /// <summary>
    /// Rewind the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rewind();

    /// <summary>
    /// Try to resume the partition.
    /// </summary>
    /// <param name="eventSourceId">The partition to try to resume.</param>
    /// <returns>Awaitable task.</returns>
    Task TryResumePartition(EventSourceId eventSourceId);

    /// <summary>
    /// Notify that catch-up is complete.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task NotifyCatchUpComplete();
    
    /// <summary>
    /// Notify that failed partition has run to completion.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task NotifyFailedPartitionRecoveryComplete(EventSequenceNumber lastProcessedEvent);

    /// <summary>
    /// Notify that the partition has failed.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> that caused the failure.</param>
    /// <param name="exceptionMessages">All exception messages.</param>
    /// <param name="exceptionStackTrace">The exception stacktrace.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionFailed(AppendedEvent @event, IEnumerable<string> exceptionMessages, string exceptionStackTrace);

    /// <summary>
    /// Gets the type of the Subscriber
    /// </summary>
    /// <returns>Subscriber Type</returns>
    Task<Type> GetSubscriberType();
}
