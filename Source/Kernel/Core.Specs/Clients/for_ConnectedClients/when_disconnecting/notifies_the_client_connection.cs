// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class notifies_the_client_connection : given.a_connection_with_projection_declarations
{
    Task Because() => _clients.OnClientDisconnected(_connection, "closed");

    [Fact] void should_disconnect_the_client_connection() => _clientConnection.Received(1).Disconnect();
}
