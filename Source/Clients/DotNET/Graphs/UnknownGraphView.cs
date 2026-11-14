// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a graph has no valid view with a given name.
/// </summary>
/// <param name="graph">The name of the graph.</param>
/// <param name="view">The name of the view.</param>
public class UnknownGraphView(string graph, string view)
    : Exception($"The graph '{graph}' has no valid view named '{view}'.");
