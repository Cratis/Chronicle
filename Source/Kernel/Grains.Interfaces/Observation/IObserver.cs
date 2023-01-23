// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines an observer of an event sequence.
/// </summary>
public interface IObserver : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Set metadata associated with the observer.
    /// </summary>
    /// <param name="name">Friendly name of the observer.</param>
    /// <param name="type"><see cref="ObserverType"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task SetMetadata(ObserverName name, ObserverType type);

    /// <summary>
    /// Subscribe to observer.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">Type of <see cref="IObserverSubscriber"/> to subscribe.</typeparam>
    /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> to subscribe to.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe<TObserverSubscriber>(IEnumerable<EventType> eventTypes)
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
    Task Rewind();

    /// <summary>
    /// Try to resume the partition.
    /// </summary>
    /// <param name="eventSourceId">The partition to try to resume.</param>
    /// <returns>Awaitable task.</returns>
    Task TryResumePartition(EventSourceId eventSourceId);
}
