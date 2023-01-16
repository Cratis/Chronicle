// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Defines a <see cref="IGrainObserver"/> for being notified when <see cref="IClientObserver"/> gets disconnected.
/// </summary>
public interface INotifyClientObserverDisconnected : IGrainObserver
{
   /// <summary>
    /// Called when a <see cref="IClientObserver"/> is disconnected.
    /// </summary>
    /// <param name="client">Details about the client.</param>
    void OnClientObserverDisconnected(ConnectedClient client);
}
