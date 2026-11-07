// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Exception that gets thrown when a node is deleted without detaching while edges are still attached to it.
/// </summary>
/// <param name="node">The <see cref="NodeReference"/> of the node.</param>
public class NodeStillHasEdges(NodeReference node)
    : Exception($"The node '{node.Label}' with key '{node.Key}' still has edges. Delete it with detach to delete its edges as well.");
