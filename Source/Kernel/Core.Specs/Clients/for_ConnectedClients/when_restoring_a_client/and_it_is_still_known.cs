// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_restoring_a_client;

public class and_it_is_still_known : given.a_connection_with_projection_declarations
{
    Task Because() => _clients.OnClientRestored(_connection, "1.0", false, 1, "/app", "host", ".NET");

    [Fact] async Task should_leave_it_as_it_was() => (await _clients.DeclaresProjection(_eventStore, _projection)).ShouldBeTrue();
}
