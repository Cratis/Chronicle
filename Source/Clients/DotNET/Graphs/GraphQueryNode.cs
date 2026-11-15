// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a view resolved for querying: the node it is anchored at and its members, with every relation resolved
/// to the node of its target view. It is self-contained, so an executor needs nothing else to translate it.
/// </summary>
/// <param name="View">The name of the view.</param>
/// <param name="Label">The label of the node the view is anchored at.</param>
/// <param name="KeyProperty">The name of the key property of the node. Roots and related items are ordered by it.</param>
/// <param name="Members">The <see cref="GraphQueryMember">members</see>, in declaration order.</param>
public sealed record GraphQueryNode(string View, string Label, string KeyProperty, IReadOnlyList<GraphQueryMember> Members);
