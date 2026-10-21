// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_the_client_responds;

public class and_its_connection_is_not_open_on_this_silo : given.a_connected_client
{
    void Establish() => _connectedClients.OnClientPing(new ConnectionId(TheConnection)).Returns(false);

    Task Because() => Respond();

    [Fact] Task should_not_put_the_client_back() => WasRestored(0);
}
