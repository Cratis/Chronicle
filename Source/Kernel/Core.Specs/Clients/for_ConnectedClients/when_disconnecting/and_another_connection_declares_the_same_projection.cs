// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_another_connection_declares_the_same_projection : given.a_connection_with_projection_declarations
{
    async Task Establish()
    {
        var replica = ConnectionId.New();
        await Connect(replica);
        await _clients.RecordDeclaredProjections(replica, _eventStore, [_projection]);
    }

    async Task Because() => await _clients.OnClientDisconnected(_connection, "disconnected");

    [Fact] async Task should_retain_the_replicas_declaration() => (await _clients.DeclaresProjection(_eventStore, _projection)).ShouldBeTrue();
}
