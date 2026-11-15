// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

/// <summary>
/// Represents the identity of a node in the <see cref="InMemoryGraphSink"/>.
/// </summary>
/// <param name="Label">The node label.</param>
/// <param name="Key">The text form of the node key.</param>
public record InMemoryGraphNodeId(string Label, string Key)
{
    /// <summary>
    /// Create an identity from a <see cref="NodeReference"/>.
    /// </summary>
    /// <param name="node">The reference.</param>
    /// <returns>The identity.</returns>
    public static InMemoryGraphNodeId From(NodeReference node) => new(node.Label, GraphValues.TextOf(node.Key));
}
