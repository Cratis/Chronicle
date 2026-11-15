// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Deletes a node.
/// </summary>
/// <param name="Node">The <see cref="NodeReference"/> of the node.</param>
/// <param name="Detach">
/// Whether the edges attached to the node are deleted with it. Without it, deleting a node that still has edges
/// is an error, which is the behavior of the graph engines the sinks map to.
/// </param>
public sealed record DeleteNode(NodeReference Node, bool Detach) : GraphOperation;
