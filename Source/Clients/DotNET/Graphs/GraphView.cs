// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a view over a property graph: a read model anchored at one node label.
/// </summary>
/// <param name="Name">The name of the view, unique in its graph.</param>
/// <param name="Root">The label of the node the view is anchored at.</param>
/// <param name="Members">The <see cref="GraphViewMember">members</see>, in declaration order.</param>
public sealed record GraphView(string Name, string Root, IReadOnlyList<GraphViewMember> Members);
