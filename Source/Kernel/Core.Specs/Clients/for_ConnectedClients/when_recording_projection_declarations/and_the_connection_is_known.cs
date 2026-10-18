// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_recording_projection_declarations;

public class and_the_connection_is_known : given.a_connection_with_projection_declarations
{
    async Task Because() => await _clients.RecordDeclaredProjections(_connection, _eventStore, ["another-projection", _projection]);

    [Fact] async Task should_retain_the_original_declaration() => (await _clients.DeclaresProjection(_eventStore, _projection)).ShouldBeTrue();
    [Fact] async Task should_include_the_partial_registration() => (await _clients.DeclaresProjection(_eventStore, "another-projection")).ShouldBeTrue();
    [Fact] async Task should_scope_declarations_to_the_event_store() => (await _clients.DeclaresProjection("another-store", _projection)).ShouldBeFalse();
    [Fact] async Task should_not_invent_a_connection() => (await _clients.IsConnected(ConnectionId.NotSet)).ShouldBeFalse();
}
