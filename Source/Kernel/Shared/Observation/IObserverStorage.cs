// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Observation;

/// <summary>
/// Defines a storage provider for working with observers.
/// </summary>
public interface IObserverStorage
{
    /// <summary>
    /// Get all observers for specific event types.
    /// </summary>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <returns>Collection of <see cref="ObserverId"/> identifying the observers that observe the event types.</returns>
    Task<IEnumerable<ObserverId>> GetObserversForEventTypes(IEnumerable<EventType> eventTypes);
}
