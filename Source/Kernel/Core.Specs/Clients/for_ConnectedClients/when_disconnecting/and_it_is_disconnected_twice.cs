// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_it_is_disconnected_twice : given.a_registry_that_notifies_departures
{
    async Task Establish()
    {
        await Connect(_connection);
        await _clients.RecordObserver(_connection, _first);
    }

    async Task Because()
    {
        await _clients.OnClientDisconnected(_connection, "stale");
        await _clients.OnClientDisconnected(_connection, "closed");
        await NextNotification();
    }

    [Fact] void should_notify_once() => _notifier.ReceivedWithAnyArgs(1).Notify(_connection, []);
}
