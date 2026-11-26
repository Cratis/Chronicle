// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Defines a factory for the <see cref="IClientReactorSubscription"/> of a connection's reactor.
/// </summary>
public interface IClientReactorSubscriptions
{
    /// <summary>
    /// Creates the subscription for a connection's reactor. The caller owns it for as long as the stream is open.
    /// </summary>
    /// <param name="key">The <see cref="ConnectedObserverKey"/> identifying the reactor on the connection.</param>
    /// <returns>A new <see cref="IClientReactorSubscription"/>.</returns>
    IClientReactorSubscription Create(ConnectedObserverKey key);
}
