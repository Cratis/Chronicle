// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClientsLocator;

public class when_the_connection_is_registered_on_the_preferred_silo : given.two_silos
{
    IConnectedClients? _result;

    void Establish()
    {
        _firstClients.IsConnected(_connection).Returns(true);
        _secondClients.IsConnected(_connection).Returns(true);
    }

    async Task Because() => _result = await ConnectedClientsLocator.Find(_grainFactory, _first, _connection);

    [Fact] void should_find_the_registry_of_the_preferred_silo() => _result.ShouldEqual(_firstClients);
    [Fact] void should_not_ask_the_other_silo() => _secondClients.DidNotReceive().IsConnected(Arg.Any<ConnectionId>());
}
