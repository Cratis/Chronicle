// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Defines an observer for a specific connected client in <see cref="IConnectedClients"/>.
/// </summary>
public interface IConnectedClientObserver : IGrainObserver
{
    /// <summary>
    /// Called when the client is disconnected.
    /// </summary>
    /// <param name="connectionId">The identifier of the connection.</param>
    void Disconnected(string connectionId);
}
