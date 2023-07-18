// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Defines a client observer.
/// </summary>
public interface IClientObserver : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Start the observer.
    /// </summary>
    /// <param name="name">Friendly <see cref="ObserverName"/> for the client observer.</param>
    /// <param name="connectionId">The unique identifier of the connection the client observer is for.</param>
    /// <param name="eventTypes">The <see cref="EventType">event types</see> the observer is expecting.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(ObserverName name, ConnectionId connectionId, IEnumerable<EventType> eventTypes);
}
