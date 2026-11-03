// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

public class and_the_registry_still_knows_a_client_running_with_a_debugger : given.a_connected_client
{
    void Establish() => _connectedClients.IsConnected(new ConnectionId(TheConnection)).Returns(true);

    async Task Because()
    {
        Connect(isRunningWithDebugger: true);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.IsConnected)) >= 1);
    }

    [Fact] Task should_not_put_the_client_back() => WasRestored(0);
}
