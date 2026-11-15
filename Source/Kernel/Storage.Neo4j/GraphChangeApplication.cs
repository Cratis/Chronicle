// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Applies the operations of one <see cref="GraphChangeset"/> in an open transaction.
/// </summary>
/// <remarks>
/// It makes the same decisions as the in-memory graph sink, which is the executable reference: the watermark skips a write,
/// a stale element of the replaying owner reads as absent and is replaced by the first write, an edge creates bare end nodes,
/// and an element is only handed to another owner when it has none.
/// </remarks>
/// <param name="definition">The <see cref="GraphDefinition"/> of the graph.</param>
/// <param name="cypher">The <see cref="GraphCypher"/> generating the statements.</param>
/// <param name="transaction">The open transaction.</param>
/// <param name="owner">The owner of the changeset.</param>
/// <param name="sequence">The event sequence number of the changeset.</param>
/// <param name="generation">The generation the owner writes in: the replay generation while replaying, else the current one.</param>
/// <param name="replayGeneration">The replay generation of the owner, or null when it is not replaying.</param>
public class GraphChangeApplication(
    GraphDefinition definition,
    GraphCypher cypher,
    IAsyncQueryRunner transaction,
    string owner,
    long sequence,
    long generation,
    long? replayGeneration)
{
    /// <summary>
    /// Apply an operation.
    /// </summary>
    /// <param name="operation">The <see cref="GraphOperation"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="NotSupportedException">Thrown when the operation is not one of the five graph operations.</exception>
    public async Task Apply(GraphOperation operation)
    {
        switch (operation)
        {
            case UpsertNode upsert:
                await UpsertNode(upsert);
                break;
            case SetProperties { Element: NodeReference node } set:
                await SetNodeProperties(node, set);
                break;
            case SetProperties { Element: EdgeReference edge } set:
                await SetEdgeProperties(edge, set);
                break;
            case DeleteNode delete:
                await DeleteNode(delete);
                break;
            case UpsertEdge upsert:
                await UpsertEdge(upsert);
                break;
            case DeleteEdge delete:
                await DeleteEdge(delete);
                break;
            default:
                throw new NotSupportedException($"The graph operation '{operation.GetType().Name}' is not supported.");
        }
    }

    async Task UpsertNode(UpsertNode upsert)
    {
        definition.GetNode(upsert.Node.Label);
        var stored = await GetNode(upsert.Node);
        if (stored is { } existing && !existing.IsStale(owner, replayGeneration) && existing.IsAtOrBelowWatermark(sequence))
        {
            return;
        }

        if (stored?.IsStale(owner, replayGeneration) == true)
        {
            // The first write in a replay starts the stale element over.
            await Run(cypher.MakeNodeBare(upsert.Node));
        }

        await Run(cypher.WriteNode(upsert.Node, upsert.Properties, owner, sequence, generation));
    }

    async Task SetNodeProperties(NodeReference node, SetProperties set)
    {
        definition.GetNode(node.Label);
        if (await GetNode(node) is not { } stored || stored.IsStale(owner, replayGeneration) || stored.IsAtOrBelowWatermark(sequence))
        {
            return;
        }

        await Run(cypher.SetNodeProperties(node, set.Properties, owner, sequence, generation));
    }

    async Task SetEdgeProperties(EdgeReference edge, SetProperties set)
    {
        Validate(edge);
        if (await GetEdge(edge) is not { } stored || stored.IsStale(owner, replayGeneration) || stored.IsAtOrBelowWatermark(sequence))
        {
            return;
        }

        await Run(cypher.SetEdgeProperties(edge, set.Properties, owner, sequence, generation));
    }

    async Task DeleteNode(DeleteNode delete)
    {
        definition.GetNode(delete.Node.Label);
        if (await GetNode(delete.Node) is not { } stored || stored.IsStale(owner, replayGeneration) || stored.IsAtOrBelowWatermark(sequence))
        {
            return;
        }

        if (stored.Edges > 0 && !delete.Detach)
        {
            throw new NodeStillHasEdges(delete.Node);
        }

        await Run(cypher.DeleteNode(delete.Node, delete.Detach));
    }

    async Task UpsertEdge(UpsertEdge upsert)
    {
        Validate(upsert.Edge);
        var stored = await GetEdge(upsert.Edge);
        if (stored is { } existing && !existing.IsStale(owner, replayGeneration) && existing.IsAtOrBelowWatermark(sequence))
        {
            return;
        }

        await EnsureEndpoint(upsert.Edge.Source);
        await EnsureEndpoint(upsert.Edge.Target);
        if (stored?.IsStale(owner, replayGeneration) == true)
        {
            await Run(cypher.DeleteEdge(upsert.Edge));
        }

        await Run(cypher.WriteEdge(upsert.Edge, upsert.Properties, owner, sequence, generation));
    }

    async Task DeleteEdge(DeleteEdge delete)
    {
        Validate(delete.Edge);
        if (await GetEdge(delete.Edge) is not { } stored || stored.IsStale(owner, replayGeneration) || stored.IsAtOrBelowWatermark(sequence))
        {
            return;
        }

        await Run(cypher.DeleteEdge(delete.Edge));
    }

    async Task EnsureEndpoint(NodeReference node)
    {
        definition.GetNode(node.Label);
        var stored = await GetNode(node);
        if (stored is null)
        {
            await Run(cypher.EnsureEndpoint(node));
        }
        else if (stored.IsStale(owner, replayGeneration))
        {
            // The edge needs the node, so it must outlive the sweep. It becomes a bare node that the node mapping can claim again.
            await Run(cypher.MakeNodeBare(node));
        }
    }

    void Validate(EdgeReference edge)
    {
        var edgeDefinition = definition.GetEdge(edge.Type);
        if ((edgeDefinition.IdentifyingProperty is null) != (edge.Identity is null))
        {
            throw new InvalidOperationException($"The edge type '{edgeDefinition.Type}' {(edgeDefinition.IdentifyingProperty is null ? "does not declare" : "requires")} an identifying property.");
        }

        if (edge.Source.Label != edgeDefinition.SourceLabel || edge.Target.Label != edgeDefinition.TargetLabel)
        {
            throw new InvalidOperationException($"The edge type '{edgeDefinition.Type}' connects '{edgeDefinition.SourceLabel}' to '{edgeDefinition.TargetLabel}'.");
        }
    }

    async Task<StoredElement?> GetNode(NodeReference node)
    {
        var statement = cypher.ReadNode(node);
        var records = await Read(statement);
        return records.Count == 0 ? null : new StoredElement(records[0]["properties"].As<IReadOnlyDictionary<string, object>>(), records[0]["edges"].As<long>());
    }

    async Task<StoredElement?> GetEdge(EdgeReference edge)
    {
        var records = await Read(cypher.ReadEdge(edge));
        return records.Count == 0 ? null : new StoredElement(records[0]["properties"].As<IReadOnlyDictionary<string, object>>());
    }

    async Task<IReadOnlyList<IRecord>> Read(CypherStatement statement)
    {
        var cursor = await transaction.RunAsync(statement.Text, statement.ToDriverParameters());
        return await cursor.ToListAsync();
    }

    async Task Run(CypherStatement statement)
    {
        var cursor = await transaction.RunAsync(statement.Text, statement.ToDriverParameters());
        await cursor.ConsumeAsync();
    }
}
