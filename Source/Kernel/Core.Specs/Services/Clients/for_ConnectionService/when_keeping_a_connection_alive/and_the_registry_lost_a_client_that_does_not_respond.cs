// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

/// <summary>
/// The registry drops a client that stopped responding on purpose. Sending keep-alives proves nothing about the client,
/// so they must never put it back.
/// </summary>
public class and_the_registry_lost_a_client_that_does_not_respond : given.a_connected_client
{
    void Establish() => _connectedClients.IsConnected(new ConnectionId(TheConnection)).Returns(false);

    async Task Because()
    {
        await ConnectAndWaitForRegistration();

        // More than one keep-alive has checked the registry and found the client gone.
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.IsConnected)) >= 2);
    }

    [Fact] Task should_not_put_the_client_back() => WasRestored(0);
}
