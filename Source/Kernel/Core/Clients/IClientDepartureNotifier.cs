// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines a system that tells observers a client connection has been dropped.
/// </summary>
public interface IClientDepartureNotifier
{
    /// <summary>
    /// Tells each of the observers that the connection was dropped, so they can hand over to another client right away.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> that was dropped.</param>
    /// <param name="observers">The <see cref="ObserverKey"/> of every observer the connection subscribed.</param>
    /// <returns>Awaitable task. It does not fail; a notification that could not be delivered is left to the observer watchdog.</returns>
    Task Notify(ConnectionId connectionId, IReadOnlyCollection<ObserverKey> observers);

    /// <summary>
    /// Tells the observers that were released while dormant, and recorded the connection as their client, that it was dropped.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> that was dropped.</param>
    /// <param name="alreadyNotified">The observers <see cref="Notify"/> told, which are not told again.</param>
    /// <returns>Awaitable task. It does not fail; an observer that is not told is found by its queue's safety net.</returns>
    /// <remarks>
    /// The observers the registry remembers are only the ones it was told about while the connection lived. A dormant
    /// observer holds its client in its durable subscription, which outlives the registry's memory, so the subscriptions
    /// are read to find the observers that have to hand over (decision 0034).
    /// </remarks>
    Task NotifyDormant(ConnectionId connectionId, IReadOnlyCollection<ObserverKey> alreadyNotified);
}
