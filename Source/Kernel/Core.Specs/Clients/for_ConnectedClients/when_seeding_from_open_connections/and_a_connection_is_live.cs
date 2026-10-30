// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_seeding_from_open_connections;

public class and_a_connection_is_live : Specification
{
    ConnectedClients _clients;
    ConnectionId _connection = ConnectionId.New();
    ConnectedClient _client;

    void Establish() => _clients = new ConnectedClients(
        Substitute.For<ILogger<ConnectedClients>>(),
        Substitute.For<IMeter<ConnectedClients>>(),
        Options.Create(new ChronicleOptions()),
        Substitute.For<IStorage>());

    async Task Because()
    {
        await _clients.SeedLiveConnections([new LiveClientConnection(_connection, "2.0", true, 42, "/live", "live-host", "Live")], keyedForThisSilo: true);
        _client = await _clients.GetConnectedClient(_connection);
    }

    [Fact] async Task should_be_connected() => (await _clients.IsConnected(_connection)).ShouldBeTrue();
    [Fact] void should_keep_the_version() => _client.Version.ShouldEqual("2.0");
    [Fact] void should_keep_the_debugger_flag() => _client.IsRunningWithDebugger.ShouldBeTrue();
    [Fact] void should_keep_the_process_id() => _client.ProcessId.ShouldEqual(42);
    [Fact] void should_keep_the_process_path() => _client.ProcessPath.ShouldEqual("/live");
    [Fact] void should_keep_the_machine_name() => _client.MachineName.ShouldEqual("live-host");
    [Fact] void should_keep_the_client_type() => _client.ClientType.ShouldEqual("Live");
}
