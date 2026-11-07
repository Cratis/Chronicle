// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Graphs;

/// <summary>
/// Exception that gets thrown when a graph does not define a node label or an edge type.
/// </summary>
/// <param name="graph">The name of the graph.</param>
/// <param name="kind">The <see cref="GraphElementKind"/> that is missing.</param>
/// <param name="name">The label or type that is missing.</param>
public class UnknownGraphElement(string graph, GraphElementKind kind, string name)
    : Exception($"The graph '{graph}' does not define the {kind.ToString().ToLowerInvariant()} '{name}'.");
