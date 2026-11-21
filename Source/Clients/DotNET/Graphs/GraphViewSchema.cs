// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents the nodes and edges of a graph that its views were checked against.
/// </summary>
/// <param name="Nodes">The <see cref="GraphViewSchemaNode">nodes</see>, in declaration order.</param>
/// <param name="Edges">The <see cref="GraphViewSchemaEdge">edges</see>, in declaration order.</param>
public sealed record GraphViewSchema(IReadOnlyList<GraphViewSchemaNode> Nodes, IReadOnlyList<GraphViewSchemaEdge> Edges);
