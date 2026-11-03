// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_recording_an_observer;

public class and_the_connection_is_unknown : given.a_registry_that_notifies_departures
{
    async Task Establish() => await _clients.RecordObserver(_connection, _first);

    async Task Because()
    {
        await Connect(_connection);
        await _clients.OnClientDisconnected(_connection, "closed");
    }

    [Fact] void should_not_notify_anything() => _notifier.DidNotReceiveWithAnyArgs().Notify(_connection, []);
}
