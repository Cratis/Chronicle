// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_restoring_a_client;

public class and_it_is_unknown : given.a_connection_with_projection_declarations
{
    ConnectionId _restored = ConnectionId.New();

    IClientConnection _restoredConnection = Substitute.For<IClientConnection>();

    void Establish() => _grainFactory.GetGrain<IClientConnection>(_restored.Value).Returns(_restoredConnection);

    Task Because() => _clients.OnClientRestored(_restored, "1.0", false, 1, "/app", "host", ".NET");

    [Fact] void should_connect_the_client_connection() => _restoredConnection.Received(1).Connect();
}
