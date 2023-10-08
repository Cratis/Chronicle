// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Defines a system for tracking connected observers.
/// </summary>
public interface IConnectedClientsState
{
    /// <summary>
    /// Gets an observable with all the connected clients for specific microservice.
    /// </summary>
    /// <returns><see cref="IObservable{T}"/> of a collection of <see cref="ConnectedClient"/>.</returns>
    IObservable<IEnumerable<ConnectedClient>> GetAll();
}
