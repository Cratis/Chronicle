// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

/// <summary>
/// Defines a grain for working with all <see cref="IClientObserver">client observers</see>.
/// </summary>
public interface IClientObservers : IGrainWithGuidKey
{
    /// <summary>
    /// Register a collection of client observers.
    /// </summary>
    /// <param name="connectionId"><see cref="ConnectionId"/> to register with.</param>
    /// <param name="registrations">Collection of <see cref="ClientObserverRegistration"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(ConnectionId connectionId, IEnumerable<ClientObserverRegistration> registrations);
}
