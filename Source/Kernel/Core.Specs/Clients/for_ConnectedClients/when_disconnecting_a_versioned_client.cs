// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_disconnecting_a_versioned_client : given.a_versioned_projection_registration
{
    async Task Because() => await _clients.OnClientDisconnected(_connection, "disconnected");

    [Fact] async Task should_clear_the_registration() => (await _clients.GetProjectionRegistrations(_eventStore, _projection)).ShouldBeEmpty();
}
