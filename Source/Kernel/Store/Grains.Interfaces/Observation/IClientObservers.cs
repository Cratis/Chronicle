// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Grains.Connections;
using Aksio.Cratis.Events.Store.Observation;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Defines a system for working with client observers.
/// </summary>
public interface IClientObservers : IGrainWithGuidKey, IConnectedClientObserver
{
    /// <summary>
    /// Subscribes a specific observer to an <see cref="EventSequenceId"/> for specific event types.
    /// </summary>
    /// <param name="name">Friendly name of the observer.</param>
    /// <param name="observerId">The <see cref="ObserverId"/>.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/>.</param>
    /// <param name="eventTypes">The collection of <see cref="EventType">event types</see>.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe(ObserverName name, ObserverId observerId, EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes);
}
