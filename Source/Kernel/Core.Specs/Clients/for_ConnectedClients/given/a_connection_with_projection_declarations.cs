// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.given;

public class a_connection_with_projection_declarations : Specification
{
    protected ConnectedClients _clients;
    protected ConnectionId _connection = ConnectionId.New();
    protected EventStoreName _eventStore = "the-store";
    protected ProjectionId _projection = "the-projection";

    async Task Establish()
    {
        _clients = new ConnectedClients(Substitute.For<ILogger<ConnectedClients>>(), Substitute.For<IMeter<ConnectedClients>>(), Options.Create(new ChronicleOptions()), Substitute.For<IStorage>());
        await Connect(_connection);
        await _clients.RecordDeclaredProjections(_connection, _eventStore, [_projection]);
    }

    protected Task Connect(ConnectionId connection) => _clients.OnClientConnected(connection, "1.0", false, 1, "/app", "host", ".NET");
}
