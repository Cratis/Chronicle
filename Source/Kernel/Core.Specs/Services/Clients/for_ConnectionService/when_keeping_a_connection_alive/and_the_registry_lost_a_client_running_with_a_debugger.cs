// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

/// <summary>
/// A client running with a debugger does not respond to keep-alives and is never dropped for it, so the registry not
/// knowing it can only mean the registry - a grain activation - was lost while the connection stayed open.
/// </summary>
public class and_the_registry_lost_a_client_running_with_a_debugger : given.a_connected_client
{
    void Establish() => _connectedClients.IsConnected(new ConnectionId(TheConnection)).Returns(false, true);

    async Task Because()
    {
        Connect(isRunningWithDebugger: true);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.OnClientRestored)) >= 1);
    }

    [Fact] Task should_put_the_client_back() => WasRestored(1);
    [Fact] Task should_not_register_it_as_a_new_connection() => WasRegistered(1);
}
