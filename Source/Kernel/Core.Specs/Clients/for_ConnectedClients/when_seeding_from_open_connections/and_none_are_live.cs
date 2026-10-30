// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_seeding_from_open_connections;

public class and_none_are_live : Specification
{
    ConnectedClients _clients;

    void Establish() => _clients = new ConnectedClients(
        Substitute.For<ILogger<ConnectedClients>>(),
        Substitute.For<IMeter<ConnectedClients>>(),
        Options.Create(new ChronicleOptions()),
        Substitute.For<IStorage>());

    Task Because() => _clients.SeedLiveConnections([], keyedForThisSilo: true);

    [Fact] async Task should_stay_empty() => (await _clients.GetConnectionCount()).ShouldEqual(0);
}
