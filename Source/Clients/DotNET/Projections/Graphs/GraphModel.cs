// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents a graph as declared by the model-bound attributes or by the fluent builder, before it is turned into
/// the definitions that are registered. Both forms produce this model, which is what makes them produce identical definitions.
/// </summary>
/// <param name="Name">The name of the graph.</param>
/// <param name="Nodes">The nodes.</param>
/// <param name="Edges">The edges.</param>
internal record GraphModel(string Name, IReadOnlyList<GraphNodeModel> Nodes, IReadOnlyList<GraphEdgeModel> Edges);
