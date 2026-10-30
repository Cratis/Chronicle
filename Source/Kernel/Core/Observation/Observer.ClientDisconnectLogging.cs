// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Logs what a client disconnect does to this observer's subscription, with the connection ids involved,
    /// so a subscription that disappears can be traced from Information-level logs.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client that disconnected.</param>
    /// <param name="outcome">What the observer does about it.</param>
    void LogClientDisconnected(ConnectionId connectionId, string outcome)
    {
        var subscribed = _subscription.Targets.Count > 0
            ? string.Join(", ", _subscription.Targets.Select(target => target.ConnectedClient?.ConnectionId.ToString() ?? "<none>"))
            : (_subscription.Arguments as ConnectedClient)?.ConnectionId.ToString() ?? "<none>";
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.ClientDisconnectedFromObserver(connectionId, subscribed, outcome);
    }
}
