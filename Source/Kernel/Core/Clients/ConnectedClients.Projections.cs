// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Clients;

public partial class ConnectedClients
{
    /// <inheritdoc/>
    public Task RecordDeclaredProjections(ConnectionId connectionId, EventStoreName eventStore, IEnumerable<ProjectionId> projections)
    {
        if (_clients.TryGetValue(connectionId, out var entry))
        {
            entry.Projections.UnionWith(projections.Select(projection => (eventStore, projection)));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> DeclaresProjection(EventStoreName eventStore, ProjectionId projection) =>
        Task.FromResult(_clients.Values.Any(entry => entry.Projections.Contains((eventStore, projection))));

    sealed record ConnectionEntry(ConnectedClient Client)
    {
        public HashSet<(EventStoreName EventStore, ProjectionId Projection)> Projections { get; } = [];
    }
}
