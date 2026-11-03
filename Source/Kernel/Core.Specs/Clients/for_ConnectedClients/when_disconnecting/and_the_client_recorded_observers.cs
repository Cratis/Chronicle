// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_the_client_recorded_observers : given.a_registry_that_notifies_departures
{
    ObserverKey[] _notifiedObservers;

    async Task Establish()
    {
        await Connect(_connection);
        await Connect(_otherConnection);
        await _clients.RecordObserver(_connection, _first);
        await _clients.RecordObserver(_connection, _second);
        await _clients.RecordObserver(_otherConnection, _third);
    }

    async Task Because()
    {
        await _clients.OnClientDisconnected(_connection, "closed");
        (_, _notifiedObservers) = await NextNotification();
    }

    [Fact] void should_notify_only_the_observers_of_that_connection() => _notifiedObservers.ShouldContainOnly(_first, _second);
    [Fact] void should_notify_once() => _notifier.ReceivedWithAnyArgs(1).Notify(_connection, []);
    [Fact] async Task should_keep_the_other_connection() => (await _clients.IsConnected(_otherConnection)).ShouldBeTrue();
}
