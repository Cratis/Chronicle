// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_client_connects : Specification
{
    IClientConnection _clientConnection = Substitute.For<IClientConnection>();
    ConnectedClients _connectedClients = default!;
    ConnectionId _connectionId = default!;
    ConnectedClient _client = default!;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        _connectedClients = new ConnectedClients(
            grainFactory,
            Substitute.For<ILogger<ConnectedClients>>(),
            Substitute.For<IMeter<ConnectedClients>>(),
            Options.Create(new ChronicleOptions()),
            Substitute.For<IStorage>());
        _connectionId = ConnectionId.New();
        grainFactory.GetGrain<IClientConnection>(_connectionId.Value).Returns(_clientConnection);
    }

    async Task Because()
    {
        await _connectedClients.OnClientConnected(_connectionId, "2.3.4", false, 4242, "/apps/my-app/MyApp", "worker-1", ".NET");
        _client = await _connectedClients.GetConnectedClient(_connectionId);
    }

    [Fact] void should_connect_the_client_connection() => _clientConnection.Received(1).Connect();
    [Fact] void should_store_the_version() => _client.Version.ShouldEqual("2.3.4");
    [Fact] void should_store_the_process_id() => _client.ProcessId.ShouldEqual(4242);
    [Fact] void should_store_the_process_path() => _client.ProcessPath.ShouldEqual("/apps/my-app/MyApp");
    [Fact] void should_store_the_machine_name() => _client.MachineName.ShouldEqual("worker-1");
    [Fact] void should_store_the_client_type() => _client.ClientType.ShouldEqual(".NET");
}
