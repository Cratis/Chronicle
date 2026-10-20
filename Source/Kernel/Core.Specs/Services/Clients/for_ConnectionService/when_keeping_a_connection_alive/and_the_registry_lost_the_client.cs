// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

/// <summary>
/// The registry of connected clients lives in a grain activation, which can be replaced while the connection is
/// still open - a client nobody registers again would be treated as gone by every observer it is subscribed to.
/// </summary>
public class and_the_registry_lost_the_client : given.a_connected_client
{
    void Establish() => _connectedClients.IsConnected(new ConnectionId(TheConnection)).Returns(false);

    async Task Because()
    {
        Connect();
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.OnClientConnected)) >= 2);
    }

    [Fact] Task should_register_the_client_again() => WasRegistered(2);
}
