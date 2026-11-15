// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphSink"/> that stores the graph in a Neo4j database.
/// </summary>
/// <remarks>
/// <para>
/// The in-memory graph sink is the executable reference for the semantics, and this sink makes the same decisions. Every
/// <see cref="GraphChangeset"/> is applied in one transaction, so it is atomic: when a statement fails, the transaction is
/// rolled back and the error reaches the caller. Nothing is caught and continued.
/// </para>
/// <para>
/// Every element carries bookkeeping properties, named by <see cref="GraphBookkeeping"/>: the watermark, the owner and the
/// replay generation, and in single database mode the namespace. The sink writes and reads in the database named for the
/// event store and namespace, created on first use when the configuration allows it.
/// </para>
/// </remarks>
/// <param name="definition">The <see cref="GraphDefinition"/> of the graph.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> of the graph.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> of the graph.</param>
/// <param name="connections">The <see cref="INeo4jConnections"/> resolving where the graph is.</param>
public partial class Neo4jGraphSink(
    GraphDefinition definition,
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    INeo4jConnections connections) : IGraphSink
{
    readonly GraphCypher _cypher = new(
        definition,
        connections.Options.DatabaseMode == Neo4jDatabaseMode.Single ? @namespace.Value : null);

    readonly ConcurrentDictionary<ProjectionId, long> _replayGenerations = new();

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Neo4j;

    /// <inheritdoc/>
    public bool SupportsMultipleEdges => true;

    /// <inheritdoc/>
    public async Task EnsureSchema()
    {
        foreach (var node in definition.Nodes)
        {
            EnsureNotReserved(node.KeyProperty, node.Schema);
        }

        foreach (var edge in definition.Edges)
        {
            EnsureNotReserved(edge.IdentifyingProperty, edge.Schema);
        }

        var schema = await connections.Resolve(eventStore, @namespace, ConnectionPurpose.Schema);
        if (schema.CanCreateDatabase)
        {
            await CreateDatabaseIfMissing(schema);
        }

        await using var session = schema.OpenSession();
        await session.ExecuteWriteAsync(async transaction =>
        {
            await Run(transaction, _cypher.CreateOwnerConstraint());
            foreach (var node in definition.Nodes) await Run(transaction, _cypher.CreateNodeConstraint(node));
            foreach (var edge in definition.Edges.Where(_ => _.IdentifyingProperty is not null)) await Run(transaction, _cypher.CreateEdgeIndex(edge));
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyGraphChanges(GraphChangeset changeset, EventSequenceNumber eventSequenceNumber)
    {
        var owner = changeset.Owner.Value;
        var sequence = checked((long)eventSequenceNumber.Value);
        var connection = await connections.Resolve(eventStore, @namespace, ConnectionPurpose.Runtime);
        await using var session = connection.OpenSession();
        await session.ExecuteWriteAsync(async transaction =>
        {
            var replay = _replayGenerations.TryGetValue(changeset.Owner, out var generation) ? generation : (long?)null;
            var current = replay ?? await ReadGeneration(transaction, owner);
            var application = new GraphChangeApplication(definition, _cypher, transaction, owner, sequence, current, replay);
            foreach (var operation in changeset.Operations)
            {
                await application.Apply(operation);
            }
        });

        return [];
    }

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => Task.FromResult<IEnumerable<FailedPartition>>([]);

    static async Task Run(IAsyncQueryRunner transaction, CypherStatement statement)
    {
        var cursor = await transaction.RunAsync(statement.Text, statement.ToDriverParameters());
        await cursor.ConsumeAsync();
    }

    static void EnsureNotReserved(string? name, Cratis.Chronicle.Schemas.JsonSchema schema)
    {
        if (name is not null) GraphBookkeeping.EnsureNotReserved(name);
        foreach (var property in schema.Properties.Keys) GraphBookkeeping.EnsureNotReserved(property);
    }

    async Task<long> ReadGeneration(IAsyncQueryRunner transaction, string owner)
    {
        var statement = _cypher.ReadGeneration(owner);
        var cursor = await transaction.RunAsync(statement.Text, statement.ToDriverParameters());
        var records = await cursor.ToListAsync();
        return records.Count == 0 || records[0]["generation"] is null ? 0 : records[0]["generation"].As<long>();
    }

    async Task CreateDatabaseIfMissing(Neo4jConnection connection)
    {
        await using var session = connection.OpenSystemSession();
        var cursor = await session.RunAsync($"CREATE DATABASE {GraphCypher.Escape(connection.Database)} IF NOT EXISTS WAIT");
        await cursor.ConsumeAsync();
    }
}
