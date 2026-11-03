// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_revising_stale_clients;

/// <summary>
/// A client running with a debugger never answers keep-alives; the connection service refreshes it through
/// <see cref="ConnectedClients.OnClientPing"/> while its connection is open, which must keep its declarations.
/// </summary>
public class and_a_debugger_client_is_refreshed_by_its_open_connection : given.a_connected_clients_with_stale_threshold
{
    readonly EventStoreName _eventStore = "the-store";
    readonly ProjectionId _projection = "the-projection";

    async Task Establish()
    {
        await Connect(true);
        await _clients.RecordDeclaredProjections(_connection, _eventStore, [_projection]);
        await MakeStale();
        await _clients.OnClientPing(_connection);
    }

    Task Because() => Revise();

    [Fact] async Task should_keep_the_client() => (await _clients.IsConnected(_connection)).ShouldBeTrue();
    [Fact] async Task should_keep_its_projection_declarations() => (await _clients.DeclaresProjection(_eventStore, _projection)).ShouldBeTrue();
}
