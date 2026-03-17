// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

/// <summary>
/// Reusable context that mocks all ConnectionService dependencies.
/// </summary>
public class a_connection_service : Specification
{
    protected IConnectionService _service;
    protected IGrainFactory _grainFactory;
    protected IClientConnectionManager _connectionManager;
    protected IConnectedClients _connectedClients;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _connectionManager = Substitute.For<IClientConnectionManager>();
        _connectedClients = Substitute.For<IConnectedClients>();

        _grainFactory.GetGrain<IConnectedClients>(0, null).Returns(_connectedClients);
        _connectionManager.WaitUntilAcceptingConnections().Returns(Task.CompletedTask);

        _service = new ConnectionService(
            _grainFactory,
            _connectionManager,
            NullLogger<ConnectionService>.Instance);
    }

    protected static ConnectRequest CreateRequest(string? connectionId = null) =>
        new()
        {
            ConnectionId = connectionId ?? Guid.NewGuid().ToString(),
            ClientVersion = "1.0.0",
            IsRunningWithDebugger = false
        };
}
