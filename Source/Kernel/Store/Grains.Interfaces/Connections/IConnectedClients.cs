// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Defines a system for tracking connected observers.
/// </summary>
public interface IConnectedClients : IGrainWithGuidKey
{
    /// <summary>
    /// Report that a client was connected.
    /// </summary>
    /// <param name="clientInformation">The information related to the client.</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientConnected(ClientInformation clientInformation);

    /// <summary>
    /// Report that a client was disconnected.
    /// </summary>
    /// <param name="microserviceId">The microservice that is disconnecting a client connection.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientDisconnected(MicroserviceId microserviceId, ConnectionId connectionId);
}
