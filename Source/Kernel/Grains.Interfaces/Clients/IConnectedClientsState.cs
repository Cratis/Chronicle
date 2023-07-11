// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Defines a system for tracking connected observers.
/// </summary>
public interface IConnectedClientsState
{
    /// <summary>
    /// Gets an observable with all the connected clients for specific microservice.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to get for.</param>
    /// <returns><see cref="IObservable{T}"/> of a collection of <see cref="ConnectedClient"/>.</returns>
    IObservable<IEnumerable<ConnectedClient>> GetAllForMicroservice(MicroserviceId microserviceId);
}
