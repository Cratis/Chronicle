// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_the_client_recorded_no_observers : given.a_registry_that_notifies_departures
{
    async Task Establish() => await Connect(_connection);

    async Task Because() => await _clients.OnClientDisconnected(_connection, "closed");

    [Fact] void should_not_notify_anything() => _notifier.DidNotReceiveWithAnyArgs().Notify(_connection, []);
}
