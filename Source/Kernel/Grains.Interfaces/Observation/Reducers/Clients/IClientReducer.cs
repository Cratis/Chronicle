// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Observation.Reducers;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Defines a client reducer.
/// </summary>
public interface IClientReducer : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Start the observer.
    /// </summary>
    /// <param name="name">Friendly <see cref="ObserverName"/> for the client observer.</param>
    /// <param name="connectionId">The unique identifier of the connection the client observer is for.</param>
    /// <param name="eventTypes">The <see cref="EventType">event types</see> the observer is expecting.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(ObserverName name, ConnectionId connectionId, IEnumerable<EventTypeWithKeyExpression> eventTypes);
}
