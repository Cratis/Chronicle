// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs;

/// <summary>
/// Exception that gets thrown when a graph scenario is asked for a node or an edge that no discovered graph declares.
/// </summary>
/// <param name="name">The node label or edge type.</param>
public class NoGraphDeclaresElement(string name)
    : Exception($"No graph declares the node label or edge type '{name}'. Make sure the node and edge types, or the graph projection, are discoverable by the client artifacts the scenario uses.");
