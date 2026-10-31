// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_client_connects_and_the_client_connection_fails : Specification
{
    ConnectedClients _connectedClients;
    ConnectionId _connectionId = ConnectionId.New();
    Exception _exception;
    bool _isConnected;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var clientConnection = Substitute.For<IClientConnection>();
        clientConnection.Connect().Returns(_ => throw new InvalidOperationException("boom"));
        grainFactory.GetGrain<IClientConnection>(_connectionId.Value).Returns(clientConnection);
        _connectedClients = new ConnectedClients(
            grainFactory,
            Substitute.For<ILogger<ConnectedClients>>(),
            Substitute.For<IMeter<ConnectedClients>>(),
            Options.Create(new ChronicleOptions()),
            Substitute.For<IStorage>());
    }

    async Task Because()
    {
        _exception = await Catch.Exception(() => _connectedClients.OnClientConnected(_connectionId, "1.0", false, 1, "/app", "host", ".NET"));
        _isConnected = await _connectedClients.IsConnected(_connectionId);
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_register_the_client() => _isConnected.ShouldBeTrue();
}
