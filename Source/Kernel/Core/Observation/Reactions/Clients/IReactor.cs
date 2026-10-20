// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Defines a client observer.
/// </summary>
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
    /// </remarks>
    /// <returns>Awaitable task.</returns>
    Task KeepSubscription();
}
