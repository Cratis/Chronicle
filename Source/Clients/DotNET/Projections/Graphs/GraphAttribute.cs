// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Names the graph that nodes and edges belong to. Put it on a node or edge type, or on the assembly to name the graph
/// for everything in it. Without it the graph is named after the assembly's root namespace.
/// </summary>
/// <param name="name">The name of the graph.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly)]
public sealed class GraphAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the graph.
    /// </summary>
    public string Name { get; } = name;
}
