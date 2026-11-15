// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Marks a type as an edge in a property graph.
/// </summary>
/// <param name="type">The edge type, for example MEMBER_OF.</param>
/// <remarks>
/// The members marked as source and target hold the keys of the nodes at both ends and are not properties of the edge.
/// An edge is identified by its type, source and target. Give a member the key attribute to identify it by that property
/// as well, which allows more than one edge of the type between the same two nodes.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class EdgeAttribute(string type) : Attribute
{
    /// <summary>
    /// Gets the edge type.
    /// </summary>
    public string Type { get; } = type;
}
