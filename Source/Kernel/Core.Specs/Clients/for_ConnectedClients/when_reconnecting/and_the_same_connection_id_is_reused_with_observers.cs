// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_reconnecting;

public class and_the_same_connection_id_is_reused_with_observers : given.a_registry_that_notifies_departures
{
    ObserverKey[] _notifiedObservers;

    async Task Establish()
    {
        await Connect(_connection);
        await _clients.RecordObserver(_connection, _first);
    }

    async Task Because()
    {
        await Connect(_connection);
        await _clients.OnClientDisconnected(_connection, "closed");
        (_, _notifiedObservers) = await NextNotification();
    }

    [Fact] void should_still_notify_the_observer_recorded_before_the_reconnect() => _notifiedObservers.ShouldContainOnly(_first);
}
