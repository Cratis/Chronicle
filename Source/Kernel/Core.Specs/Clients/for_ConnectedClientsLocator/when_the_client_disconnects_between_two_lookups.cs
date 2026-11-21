// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClientsLocator;

public class when_the_client_disconnects_between_two_lookups : given.two_silos
{
    IConnectedClients? _before;
    IConnectedClients? _after;

    void Establish() => _firstClients.IsConnected(_connection).Returns(true);

    async Task Because()
    {
        _before = await ConnectedClientsLocator.Find(_grainFactory, _first, _connection);
        _firstClients.IsConnected(_connection).Returns(false);
        _secondClients.IsConnected(_connection).Returns(false);
        _after = await ConnectedClientsLocator.Find(_grainFactory, _first, _connection);
    }

    [Fact] void should_find_the_client_while_connected() => _before.ShouldEqual(_firstClients);
    [Fact] void should_notice_the_disconnect_on_the_next_lookup() => _after.ShouldBeNull();
}
