// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_getting_connection_count_with_a_reservation_and_a_connected_client : Specification
{
    ConnectedClients _connectedClients = default!;
    int _count;

    void Establish()
    {
        _connectedClients = new ConnectedClients(
            Substitute.For<ILogger<ConnectedClients>>(),
            Substitute.For<IMeter<ConnectedClients>>(),
            Options.Create(new ChronicleOptions()));
    }

    async Task Because()
    {
        await _connectedClients.ReserveConnection();
        await _connectedClients.OnClientConnected(ConnectionId.New(), "1.0.0", false, 1234, "/path/to/client", "machine", ".NET");
        _count = await _connectedClients.GetConnectionCount();
    }

    [Fact] void should_not_double_count_the_reservation_the_connection_fulfilled() => _count.ShouldEqual(1);
}
