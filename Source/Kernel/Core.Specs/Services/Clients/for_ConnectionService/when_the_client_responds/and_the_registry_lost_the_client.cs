// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_the_client_responds;

/// <summary>
/// The registry of connected clients lives in a grain activation, which can be replaced while the connection is still
/// open - a client that keeps responding would otherwise be treated as gone by every observer it is subscribed to.
/// </summary>
public class and_the_registry_lost_the_client : given.a_connected_client
{
    void Establish() => _connectedClients.OnClientPing(new ConnectionId(TheConnection)).Returns(false);

    async Task Because()
    {
        await ConnectAndWaitForRegistration();
        await Respond();
    }

    [Fact] Task should_put_the_client_back() => WasRestored(1);
    [Fact] Task should_not_register_it_as_a_new_connection() => WasRegistered(1);
}
