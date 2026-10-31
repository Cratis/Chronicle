// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_recording_projection_declarations;

public class and_the_connection_is_unknown : given.a_connection_with_projection_declarations
{
    async Task Because() => await _clients.RecordDeclaredProjections(ConnectionId.New(), _eventStore, ["unknown-projection"]);

    [Fact] async Task should_ignore_the_declaration() => (await _clients.DeclaresProjection(_eventStore, "unknown-projection")).ShouldBeFalse();
}
