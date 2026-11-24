// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Defines a factory for the <see cref="IClientReducerSubscription"/> of a connection's reducer.
/// </summary>
public interface IClientReducerSubscriptions
{
    /// <summary>
    /// Creates the subscription for a connection's reducer. The caller owns it for as long as the stream is open.
    /// </summary>
    /// <param name="key">The <see cref="ConnectedObserverKey"/> identifying the reducer on the connection.</param>
    /// <returns>A new <see cref="IClientReducerSubscription"/>.</returns>
    IClientReducerSubscription Create(ConnectedObserverKey key);
}
