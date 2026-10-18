// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_disconnecting;

public class and_the_client_declared_projections : given.a_connection_with_projection_declarations
{
    async Task Because() => await _clients.OnClientDisconnected(_connection, "disconnected");

    [Fact] async Task should_drop_its_declarations() => (await _clients.DeclaresProjection(_eventStore, _projection)).ShouldBeFalse();
}
