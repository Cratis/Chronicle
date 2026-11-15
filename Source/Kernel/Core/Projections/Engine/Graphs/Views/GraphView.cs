// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents a read model over a property graph: anchored at one node label, its members read properties of the node,
/// relations along edges to other views, or properties of the edge that was followed.
/// </summary>
/// <param name="Name">The name of the view, unique within a graph.</param>
/// <param name="Root">The label of the node the view is anchored at.</param>
/// <param name="Members">The members of the view, in declaration order.</param>
public record GraphView(string Name, string Root, IReadOnlyList<GraphViewMember> Members);
