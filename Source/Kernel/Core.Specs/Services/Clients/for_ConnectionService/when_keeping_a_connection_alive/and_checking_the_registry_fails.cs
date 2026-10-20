// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

/// <summary>
/// A silo that is still leaving the cluster can fail a call or two; that must not end the connection.
/// </summary>
public class and_checking_the_registry_fails : given.a_connected_client
{
    void Establish() => _connectedClients.IsConnected(new ConnectionId(TheConnection))
        .Returns(
            _ => Task.FromException<bool>(new InvalidOperationException("The silo is leaving")),
            _ => Task.FromResult(false),
            _ => Task.FromResult(true));

    async Task Because()
    {
        Connect(isRunningWithDebugger: true);
        await WaitUntil(() => CallsTo(nameof(IConnectedClients.OnClientRestored)) >= 1);
    }

    [Fact] Task should_check_again_at_the_next_keep_alive() => WasRestored(1);
}
