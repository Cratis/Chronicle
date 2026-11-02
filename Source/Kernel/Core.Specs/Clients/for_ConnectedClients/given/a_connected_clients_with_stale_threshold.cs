// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.given;

public class a_connected_clients_with_stale_threshold : Specification
{
    protected ConnectedClients _clients = default!;
    protected ConnectionId _connection = ConnectionId.New();

    void Establish() => _clients = new ConnectedClients(
        Substitute.For<ILogger<ConnectedClients>>(),
        Substitute.For<IMeter<ConnectedClients>>(),
        Options.Create(new ChronicleOptions()),
        Substitute.For<IStorage>());

    protected Task Connect(bool isRunningWithDebugger) =>
        _clients.OnClientConnected(_connection, "1.0", isRunningWithDebugger, 1, "/app", "host", ".NET");

    protected async Task MakeStale() => (await _clients.GetConnectedClient(_connection)).LastSeen = DateTimeOffset.UtcNow.AddDays(-1);

    protected Task Revise() => (Task)typeof(ConnectedClients)
        .GetMethod("ReviseConnectedClients", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(_clients, [CancellationToken.None])!;
}
