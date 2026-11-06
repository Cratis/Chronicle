// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents how an event deletes a node.
/// </summary>
/// <param name="Key"><see cref="PropertyExpression"/> resolving the key of the node from the event.</param>
/// <param name="Detach">Whether the edges attached to the node are deleted with it. A node that still has edges cannot be deleted without this.</param>
/// <param name="ForEach">Optional collection property of the event; the mapping runs once per item and expressions address the item as <c language="csharp">collection.property</c>.</param>
public record GraphNodeRemovedWithDefinition(PropertyExpression Key, bool Detach, PropertyExpression? ForEach = null);
