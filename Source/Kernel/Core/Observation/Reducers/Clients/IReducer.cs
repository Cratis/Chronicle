// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Defines a client reducer.
/// </summary>
public interface IReducer : IGrainWithStringKey
{
    /// <summary>
    /// Set the reducer definition and subscribe as an observer.
    /// </summary>
    /// <param name="definition"><see cref="ReducerDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinitionAndSubscribe(ReducerDefinition definition);

    /// <summary>
    /// Unsubscribe the reducer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();

    /// <summary>
    /// Keep the subscription of the connected client in place, restoring it if the observer has lost it.
    /// </summary>
    /// <remarks>
    /// The observer's subscription lives in its activation only, and an activation can be lost while the client stays
    /// connected - with the silo hosting it, or when Orleans deactivates it because the silo owning its directory entry
    /// left the cluster. The connection that registered the client calls this periodically for as long as it is open,
    /// which is what puts the client back. A healthy check is one read-only call to the observer.
    /// <para>
    /// The definition is the one the connection registered with, not the stored one: the stored definition is shared by
    /// every connection registering the same observer, and with two client versions connected at once - a rolling
    /// deployment - it can be the other connection's. A client the connected clients registry does not know is left
    /// alone; it was disconnected or evicted for not responding, and putting it back is not this method's call.
    /// </para>
    /// </remarks>
    /// <param name="definition">The <see cref="ReducerDefinition"/> the connection registered with.</param>
    /// <returns>Awaitable task.</returns>
    Task KeepSubscription(ReducerDefinition definition);
}
