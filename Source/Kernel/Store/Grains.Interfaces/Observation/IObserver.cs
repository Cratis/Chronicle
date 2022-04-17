// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Defines an observer of an event sequence.
/// </summary>
public interface IObserver : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Rewind the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rewind();

    /// <summary>
    /// Subscribe to observer.
    /// </summary>
    /// <param name="name">Friendly name of the observer.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> to subscribe to.</param>
    /// <param name="observerNamespace">The namespace to use for handler stream.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe(ObserverName name, IEnumerable<EventType> eventTypes, ObserverNamespace observerNamespace);

    /// <summary>
    /// Unsubscribe from the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();

    /// <summary>
    /// Try to resume the partition.
    /// </summary>
    /// <param name="eventSourceId">The partition to try to resume.</param>
    /// <returns>Awaitable task.</returns>
    Task TryResumePartition(EventSourceId eventSourceId);
}
