// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_reserving_a_connection : Specification
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
        await _connectedClients.ReserveConnection();
        _count = await _connectedClients.GetConnectionCount();
    }

    [Fact] void should_count_each_reservation() => _count.ShouldEqual(2);
}
