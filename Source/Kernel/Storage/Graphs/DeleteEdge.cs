// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Deletes an edge. The end nodes are left in place.
/// </summary>
/// <param name="Edge">The <see cref="EdgeReference"/> of the edge.</param>
public sealed record DeleteEdge(EdgeReference Edge) : GraphOperation;
