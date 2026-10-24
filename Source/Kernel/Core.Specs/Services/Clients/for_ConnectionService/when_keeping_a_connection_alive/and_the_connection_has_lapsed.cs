// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

public class and_the_connection_has_lapsed : given.a_connected_client
{
    async Task Because()
    {
        Connect();
        await WaitUntil(() => KeepAlivesSent >= 1);
        Lapse();
        await WaitUntil(() => Completed && CallsTo(nameof(IConnectedClients.OnClientDisconnected)) >= 1);
    }

    [Fact] void should_complete_the_stream() => Completed.ShouldBeTrue();
    [Fact] void should_stop_sending_keep_alives() => KeepAlivesSent.ShouldEqual(1);
    [Fact] async Task should_disconnect_the_client() => await _connectedClients.Received(1).OnClientDisconnected(new ConnectionId(TheConnection), "Client disconnected");
}
