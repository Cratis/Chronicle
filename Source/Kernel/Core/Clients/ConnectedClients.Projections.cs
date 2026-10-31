// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Generations.Deployment;

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

    /// <inheritdoc/>
    public Task RecordDeclaredProjectionVersions(ConnectionId connectionId, EventStoreName eventStore, IEnumerable<ProjectionId> projections, string? registeredVersion)
    {
        if (_clients.TryGetValue(connectionId, out var entry))
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var projection in projections)
            {
                var key = (eventStore, projection);
                entry.Projections.Add(key);
                if (!entry.Registrations.TryGetValue(key, out var previous) || previous.RegisteredVersion != registeredVersion)
                {
                    entry.Registrations[key] = new(connectionId, eventStore, projection, registeredVersion, now);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionRegistration>> GetProjectionRegistrations(EventStoreName eventStore, ProjectionId projection) =>
        Task.FromResult<IEnumerable<ProjectionRegistration>>(_clients.Values
            .Where(entry => entry.Projections.Contains((eventStore, projection)))
            .Select(entry => entry.Registrations.GetValueOrDefault((eventStore, projection))
                ?? new ProjectionRegistration(entry.Client.ConnectionId, eventStore, projection, null, DateTimeOffset.MinValue))
            .ToArray());

    sealed record ConnectionEntry(ConnectedClient Client)
    {
        public HashSet<(EventStoreName EventStore, ProjectionId Projection)> Projections { get; } = [];

        public Dictionary<(EventStoreName EventStore, ProjectionId Projection), ProjectionRegistration> Registrations { get; } = [];
    }
}
