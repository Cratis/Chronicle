// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// The exception that is thrown when the nodes and edges of a model-bound graph name different sinks.
/// </summary>
/// <param name="graph">The name of the graph.</param>
/// <param name="sinks">The sinks named.</param>
public class GraphHasConflictingSinks(string graph, IEnumerable<string> sinks)
    : Exception($"The nodes and edges of graph '{graph}' name different sinks: {string.Join(", ", sinks.Select(_ => $"'{_}'"))}. A graph is stored in one sink, so put the same [Sink] on all of them.");
