// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Defines an observer for a specific connected client in <see cref="IConnectedClients"/>.
/// </summary>
public interface IConnectedClientObserver : IGrainObserver
{
    /// <summary>
    /// Called when the client is disconnected.
    /// </summary>
    void Disconnected();
}
