// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Creates a node when it does not exist and sets the given properties on it. A node that an edge created without
/// properties is completed by this operation.
/// </summary>
/// <param name="Node">The <see cref="NodeReference"/> of the node.</param>
/// <param name="Properties">The properties to set.</param>
public sealed record UpsertNode(NodeReference Node, IReadOnlyDictionary<string, object?> Properties) : GraphOperation;
