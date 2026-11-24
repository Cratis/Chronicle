// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Defines the subscription of one client reactor on one connection.
/// </summary>
/// <remarks>
/// The subscription lives with the connection's observe stream on the silo that terminates it. It is not a grain: the
/// observer owns the subscription itself, and this holds only what the stream needs to put it in place and keep it there.
/// </remarks>
public interface IClientReactorSubscription
{
    /// <summary>
    /// Store the definition and subscribe the connection to the observer.
    /// </summary>
    /// <param name="definition">The <see cref="ReactorDefinition"/> to start observing.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinitionAndSubscribe(ReactorDefinition definition);

    /// <summary>
    /// Unsubscribe the connection from the observer, unless the observer has since been taken over by another client.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();

    /// <summary>
    /// Keep the subscription of the connected client in place, restoring it if the observer has lost it.
    /// </summary>
    /// <remarks>
    /// The observer's subscription lives in its activation, and an activation can be lost while the client stays
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
    /// <param name="definition">The <see cref="ReactorDefinition"/> the connection registered with.</param>
    /// <returns>Awaitable task.</returns>
    Task KeepSubscription(ReactorDefinition definition);

    /// <summary>
    /// Checks, without the definition, that the subscription is intact.
    /// </summary>
    /// <remarks>
    /// <see cref="KeepSubscription"/> is called for every observer of every connection every couple of seconds, and its
    /// definition - every event type, filter and delivery option - is expensive to copy on each call. This is the call
    /// that is made on every tick: it answers <see langword="true"/> when the client is known to the observer and
    /// nothing is pending, and only when it answers <see langword="false"/> does the connection send the definition
    /// with <see cref="KeepSubscription"/>.
    /// </remarks>
    /// <returns><see langword="true"/> when there is nothing to restore; otherwise <see langword="false"/>.</returns>
    Task<bool> IsSubscriptionIntact();
}
