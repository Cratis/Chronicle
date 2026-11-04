// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_recording_an_observer;

public class and_the_connection_is_known : given.a_registry_that_notifies_departures
{
    ConnectionId _notifiedConnection;
    ObserverKey[] _notifiedObservers;

    async Task Establish()
    {
        await Connect(_connection);
        await _clients.RecordObserver(_connection, _first);
    }

    async Task Because()
    {
        await _clients.OnClientDisconnected(_connection, "closed");
        (_notifiedConnection, _notifiedObservers) = await NextNotification();
    }

    [Fact] void should_notify_for_the_dropped_connection() => _notifiedConnection.ShouldEqual(_connection);
    [Fact] void should_notify_the_recorded_observer() => _notifiedObservers.ShouldContainOnly(_first);
}
