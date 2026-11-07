// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

/// <summary>
/// Represents the identity of an edge in the <see cref="InMemoryGraphSink"/>.
/// </summary>
/// <param name="Type">The edge type.</param>
/// <param name="Source">The <see cref="InMemoryGraphNodeId"/> the edge starts at.</param>
/// <param name="Target">The <see cref="InMemoryGraphNodeId"/> the edge ends at.</param>
/// <param name="Identity">The text form of the identifying property value, or null when the type has none.</param>
public record InMemoryGraphEdgeId(string Type, InMemoryGraphNodeId Source, InMemoryGraphNodeId Target, string? Identity)
{
    /// <summary>
    /// Create an identity from an <see cref="EdgeReference"/>.
    /// </summary>
    /// <param name="edge">The reference.</param>
    /// <returns>The identity.</returns>
    public static InMemoryGraphEdgeId From(EdgeReference edge) => new(
        edge.Type,
        InMemoryGraphNodeId.From(edge.Source),
        InMemoryGraphNodeId.From(edge.Target),
        edge.Identity is null ? null : GraphValues.TextOf(edge.Identity));
}
