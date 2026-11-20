// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines how observers released while dormant learn that the client or the silo they recorded is gone (decision 0034).
/// </summary>
/// <remarks>
/// A dormant observer has no activation to run its watchdog, and the registry of connections only remembers the observers
/// it was told about for as long as its own activation lives. What survives both is the observer's durable subscription,
/// which names the connection and the silo of every client holding it. The sweeps read those names, never an observer.
/// </remarks>
public interface IDormantTargetSweep
{
    /// <summary>
    /// Tells every observer whose durable subscription names a connection that the connection is gone.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> that was dropped.</param>
    /// <param name="alreadyNotified">The observers that were told already, which are left alone.</param>
    /// <returns>Awaitable task. It does not fail; an observer that is not told is found by the safety net of its queue.</returns>
    /// <remarks>
    /// Connections that leave close together share one read of the subscriptions.
    /// </remarks>
    Task ConnectionLeft(ConnectionId connectionId, IReadOnlyCollection<ObserverKey> alreadyNotified);

    /// <summary>
    /// Wakes every observer whose durable subscription names a client served from a silo that left the cluster, so it drops what the silo took with it.
    /// </summary>
    /// <param name="silos">The silos, as <c language="text">SiloAddress.ToParsableString()</c> writes them, that left the cluster.</param>
    /// <returns>The number of observers woken.</returns>
    Task<int> SilosLeft(IReadOnlyCollection<string> silos);
}
