// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines a grain that wants to know when the client connection it belongs to disconnects.
/// </summary>
/// <remarks>
/// The callback is one-way, so a subscriber may call back into any grain, including the
/// <see cref="IClientConnection"/>, without risk of deadlock.
/// </remarks>
public interface IClientDisconnectionSubscriber : IGrainWithStringKey
{
    /// <summary>
    /// Called when the client connection has disconnected.
    /// </summary>
    /// <param name="epoch">The <see cref="ClientConnectionEpoch"/> the subscriber was given when subscribing.</param>
    /// <returns>Awaitable task.</returns>
    [OneWay]
    Task ClientDisconnected(ClientConnectionEpoch epoch);
}
