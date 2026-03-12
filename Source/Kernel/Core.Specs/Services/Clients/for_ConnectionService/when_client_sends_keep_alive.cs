// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService;

/// <summary>
/// Spec for verifying that a client keep-alive ping is forwarded to the grain.
/// </summary>
public class when_client_sends_keep_alive : given.a_connection_service
{
    string _connectionId;

    void Establish() => _connectionId = Guid.NewGuid().ToString();

    async Task Because() => await _service.ConnectionKeepAlive(
        new ConnectionKeepAlive { ConnectionId = _connectionId });

    [Fact]
    void should_notify_connected_clients_grain() =>
        _connectedClients.Received(1).OnClientPing(_connectionId);
}
