// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Creates an edge when it does not exist and sets the given properties on it. Both end nodes are created without
/// properties when they do not exist.
/// </summary>
/// <param name="Edge">The <see cref="EdgeReference"/> of the edge.</param>
/// <param name="Properties">The properties to set.</param>
public sealed record UpsertEdge(EdgeReference Edge, IReadOnlyDictionary<string, object?> Properties) : GraphOperation;
