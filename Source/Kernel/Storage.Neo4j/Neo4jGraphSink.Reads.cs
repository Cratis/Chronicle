// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

public partial class Neo4jGraphSink
{
    /// <inheritdoc/>
    public async Task<GraphNode?> FindNode(ProjectionId owner, NodeReference node)
    {
        var statement = _cypher.ReadNode(node);
        var records = await Query(statement);
        if (records.Count == 0) return null;

        var stored = new StoredElement(records[0]["properties"].As<IReadOnlyDictionary<string, object>>());
        return stored.IsStale(owner.Value, ReplayGenerationOf(owner)) ? null : ToNode(node, stored);
    }

    /// <inheritdoc/>
    public async Task<GraphEdge?> FindEdge(ProjectionId owner, EdgeReference edge)
    {
        var statement = _cypher.ReadEdge(edge);
        var records = await Query(statement);
        if (records.Count == 0) return null;

        var stored = new StoredElement(records[0]["properties"].As<IReadOnlyDictionary<string, object>>());
        return stored.IsStale(owner.Value, ReplayGenerationOf(owner)) ? null : ToEdge(edge, stored);
    }

    /// <inheritdoc/>
    public async Task<GraphElements<GraphNode>> GetNodes(string label, int skip = 0, int take = 50)
    {
        var node = definition.GetNode(label);
        var (count, page) = _cypher.ReadNodes(label, skip, take);
        var total = (await Query(count))[0]["total"].As<long>();
        var nodes = (await Query(page))
            .Select(_ => new StoredElement(_["properties"].As<IReadOnlyDictionary<string, object>>()))
            .Select(stored => ToNode(new NodeReference(node.Label, stored.Properties[node.KeyProperty]), stored))
            .ToArray();
        return new(nodes, total);
    }

    /// <inheritdoc/>
    public async Task<GraphElements<GraphEdge>> GetEdges(string type, int skip = 0, int take = 50)
    {
        var edge = definition.GetEdge(type);
        var (count, page) = _cypher.ReadEdges(type, skip, take);
        var total = (await Query(count))[0]["total"].As<long>();
        var edges = (await Query(page))
            .Select(record =>
            {
                var stored = new StoredElement(record["properties"].As<IReadOnlyDictionary<string, object>>());
                var identity = edge.IdentifyingProperty is { } property && stored.Properties.TryGetValue(property, out var value) ? value : null;
                var reference = new EdgeReference(
                    edge.Type,
                    new NodeReference(edge.SourceLabel, record["source"]),
                    new NodeReference(edge.TargetLabel, record["target"]),
                    identity);
                return ToEdge(reference, stored);
            })
            .ToArray();
        return new(edges, total);
    }

    static GraphNode ToNode(NodeReference reference, StoredElement stored) =>
        stored.Watermark is null
            ? new(reference, new Dictionary<string, object?>(), EventSequenceNumber.Unavailable)
            : new(reference, Neo4jValues.FromDriver(stored.Properties), (ulong)stored.Watermark.Value);

    static GraphEdge ToEdge(EdgeReference reference, StoredElement stored) =>
        new(reference, Neo4jValues.FromDriver(stored.Properties), stored.Watermark is { } watermark ? (ulong)watermark : EventSequenceNumber.Unavailable);

    long? ReplayGenerationOf(ProjectionId owner) => _replayGenerations.TryGetValue(owner, out var generation) ? generation : null;

    async Task<IReadOnlyList<IRecord>> Query(CypherStatement statement)
    {
        var connection = await connections.Resolve(eventStore, @namespace, ConnectionPurpose.Runtime);
        await using var session = connection.OpenSession(AccessMode.Read);
        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(statement.Text, statement.ToDriverParameters());
            return await cursor.ToListAsync();
        });
    }
}
