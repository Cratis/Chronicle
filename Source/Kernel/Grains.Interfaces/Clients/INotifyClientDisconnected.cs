// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Grains.Clients;

/// <summary>
/// Defines a system for observing when a client is disconnected.
/// </summary>
public interface INotifyClientDisconnected : IGrainObserver
{
    /// <summary>
    /// Called when a client is disconnected.
    /// </summary>
    /// <param name="client">Details about the client.</param>
    void OnClientDisconnected(ConnectedClient client);
}
