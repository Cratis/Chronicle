// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_client_connects : Specification
{
    ConnectedClients _connectedClients = default!;
    ConnectionId _connectionId = default!;
    ConnectedClient _client = default!;

    void Establish()
    {
        _connectedClients = new ConnectedClients(
            Substitute.For<ILogger<ConnectedClients>>(),
            Substitute.For<IMeter<ConnectedClients>>());
        _connectionId = ConnectionId.New();
    }

    async Task Because()
    {
        await _connectedClients.OnClientConnected(_connectionId, "2.3.4", false, 4242, "/apps/my-app/MyApp", "worker-1");
        _client = await _connectedClients.GetConnectedClient(_connectionId);
    }

    [Fact] void should_store_the_version() => _client.Version.ShouldEqual("2.3.4");
    [Fact] void should_store_the_process_id() => _client.ProcessId.ShouldEqual(4242);
    [Fact] void should_store_the_process_path() => _client.ProcessPath.ShouldEqual("/apps/my-app/MyApp");
    [Fact] void should_store_the_machine_name() => _client.MachineName.ShouldEqual("worker-1");
}
