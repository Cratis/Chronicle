// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

public partial class Neo4jGraphSink
{
    /// <inheritdoc/>
    public async Task BeginReplay(ProjectionId owner, ReplayContext context)
    {
        var connection = await connections.Resolve(eventStore, @namespace, ConnectionPurpose.Runtime);
        await using var session = connection.OpenSession(AccessMode.Read);
        var current = await session.ExecuteReadAsync(transaction => ReadGeneration(transaction, owner.Value));

        // The generation follows from the persisted replay context and the current generation, so a replay that is resumed
        // after a restart gets the generation it started with.
        _replayGenerations[owner] = Math.Max(context.Started.UtcTicks, current + 1);
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ProjectionId owner, ReplayContext context) =>
        _replayGenerations.ContainsKey(owner) ? Task.CompletedTask : BeginReplay(owner, context);

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> EndReplay(ProjectionId owner, ReplayContext context)
    {
        if (!_replayGenerations.TryGetValue(owner, out var generation))
        {
            return [];
        }

        var connection = await connections.Resolve(eventStore, @namespace, ConnectionPurpose.Runtime);
        await using var session = connection.OpenSession();
        await session.ExecuteWriteAsync(async transaction =>
        {
            // Edges go first. A stale node that edges of other owners still point at is kept as the bare node an edge
            // would have created, because an edge cannot exist without its end nodes. The rest is deleted.
            foreach (var statement in _cypher.SweepEdges(owner.Value, generation)) await Run(transaction, statement);
            foreach (var statement in _cypher.MakeStaleNodesWithEdgesBare(owner.Value, generation)) await Run(transaction, statement);
            foreach (var statement in _cypher.SweepNodes(owner.Value, generation)) await Run(transaction, statement);
            await Run(transaction, _cypher.WriteGeneration(owner.Value, generation));
        });

        _replayGenerations.TryRemove(owner, out _);
        return [];
    }

    /// <inheritdoc/>
    public Task LeaveReplay(ProjectionId owner)
    {
        _replayGenerations.TryRemove(owner, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task PrepareInitialRun(ProjectionId owner)
    {
        var connection = await connections.Resolve(eventStore, @namespace, ConnectionPurpose.Runtime);
        await using var session = connection.OpenSession();
        await session.ExecuteWriteAsync(async transaction =>
        {
            foreach (var statement in _cypher.RemoveOwned(owner.Value)) await Run(transaction, statement);
        });
    }
}
