// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents the graph mappings of a projection that targets a graph instead of a document or relational read model.
/// </summary>
/// <param name="Nodes">The mappings per node label.</param>
/// <param name="Edges">The mappings per edge type.</param>
/// <remarks>
/// Event types are written as "id+generation", the form produced by "EventType.ToString()", and property
/// paths as their string form, so the definition serializes the same way everywhere it is stored.
/// </remarks>
public record GraphProjectionDefinition(
    IDictionary<string, GraphNodeProjectionDefinition> Nodes,
    IDictionary<string, GraphEdgeProjectionDefinition> Edges)
{
    /// <summary>
    /// Gets a definition without any mappings.
    /// </summary>
    public static readonly GraphProjectionDefinition Empty = new(new Dictionary<string, GraphNodeProjectionDefinition>(), new Dictionary<string, GraphEdgeProjectionDefinition>());

    /// <summary>
    /// Gets whether the definition maps any event.
    /// </summary>
    public bool IsEmpty =>
        (Nodes?.Values.All(_ => _.From.Count == 0 && _.RemovedWith.Count == 0) ?? true) &&
        (Edges?.Values.All(_ => _.From.Count == 0 && _.RemovedWith.Count == 0) ?? true);
}
