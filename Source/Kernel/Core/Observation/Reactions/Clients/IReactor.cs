// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Defines a client observer.
/// </summary>
[Version(1)]
public interface IReactor : IGrainWithStringKey
{
    /// <summary>
    /// Start the observer.
    /// </summary>
    /// <param name="definition">The <see cref="ReactorDefinition"/> to start observing.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinitionAndSubscribe(ReactorDefinition definition);

    /// <summary>
    /// Unsubscribe the observer.
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
    /// <param name="definition">The <see cref="ReactorDefinition"/> the connection registered with.</param>
    /// <returns>Awaitable task.</returns>
    Task KeepSubscription(ReactorDefinition definition);

    /// <summary>
    /// Checks, without the definition, that the subscription is intact.
    /// </summary>
    /// <remarks>
    /// <see cref="KeepSubscription"/> is called for every observer of every connection every couple of seconds, and its
    /// definition - every event type, filter and delivery option - is copied through the JSON serializer on each call.
    /// With hundreds of observers per client that copying was a large part of the kernel's CPU, enough to starve
    /// connects and leave observers disconnected. This is the call that is made on every tick: it answers
    /// <see langword="true"/> when the client is known to the observer and nothing is pending, and only when it answers
    /// <see langword="false"/> does the connection send the definition with <see cref="KeepSubscription"/>.
    /// </remarks>
    /// <returns><see langword="true"/> when there is nothing to restore; otherwise <see langword="false"/>.</returns>
    Task<bool> IsSubscriptionIntact();
}
