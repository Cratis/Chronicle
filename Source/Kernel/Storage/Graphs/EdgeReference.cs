// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Represents a reference to an edge.
/// </summary>
/// <param name="Type">The type of the edge.</param>
/// <param name="Source">The <see cref="NodeReference"/> the edge starts at.</param>
/// <param name="Target">The <see cref="NodeReference"/> the edge ends at.</param>
/// <param name="Identity">
/// The value of the identifying property when the edge type declares one, otherwise null. An edge is identified by
/// its type, source and target, plus the identity when there is one.
/// </param>
public sealed record EdgeReference(string Type, NodeReference Source, NodeReference Target, object? Identity = null) : GraphElementReference;
