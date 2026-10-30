// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_seeding_from_open_connections;

public class and_the_registry_is_keyed_for_another_silo : Specification
{
    ConnectedClients _clients;
    ConnectionId _connection = ConnectionId.New();

    void Establish() => _clients = new ConnectedClients(
        Substitute.For<ILogger<ConnectedClients>>(),
        Substitute.For<IMeter<ConnectedClients>>(),
        Options.Create(new ChronicleOptions()),
        Substitute.For<IStorage>());

    Task Because() => _clients.SeedLiveConnections([new LiveClientConnection(_connection, "2.0", true, 42, "/live", "live-host", "Live")], keyedForThisSilo: false);

    [Fact] async Task should_not_seed_anything() => (await _clients.IsConnected(_connection)).ShouldBeFalse();
}
