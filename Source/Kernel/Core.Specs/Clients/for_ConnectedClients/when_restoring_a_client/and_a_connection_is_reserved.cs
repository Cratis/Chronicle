// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_restoring_a_client;

/// <summary>
/// Restoring a client the registry lost is not a new connection, so it must not fulfil a reservation made for one.
/// </summary>
public class and_a_connection_is_reserved : Specification
{
    ConnectedClients _clients;
    ConnectionId _connection = ConnectionId.New();
    int _count;

    void Establish() => _clients = new ConnectedClients(
        Substitute.For<ILogger<ConnectedClients>>(),
        Substitute.For<IMeter<ConnectedClients>>(),
        Options.Create(new ChronicleOptions()),
        Substitute.For<IStorage>());

    async Task Because()
    {
        await _clients.ReserveConnection();
        await _clients.OnClientRestored(_connection, "1.0", false, 1, "/app", "host", ".NET");
        _count = await _clients.GetConnectionCount();
    }

    [Fact] void should_count_the_reservation_and_the_restored_client() => _count.ShouldEqual(2);
    [Fact] async Task should_know_the_client() => (await _clients.IsConnected(_connection)).ShouldBeTrue();
}
