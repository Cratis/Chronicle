// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_seeding_from_open_connections;

public class and_it_is_already_known : given.a_connection_with_projection_declarations
{
    ConnectedClient _client;

    async Task Because()
    {
        await _clients.SeedLiveConnections([new LiveClientConnection(_connection, "2.0", true, 42, "/live", "live-host", "Live")], keyedForThisSilo: true);
        _client = await _clients.GetConnectedClient(_connection);
    }

    [Fact] void should_leave_it_as_it_was() => _client.Version.ShouldEqual("1.0");
    [Fact] async Task should_keep_its_projection_declarations() => (await _clients.DeclaresProjection(_eventStore, _projection)).ShouldBeTrue();
    [Fact] async Task should_not_register_it_twice() => (await _clients.GetConnectionCount()).ShouldEqual(1);
}
