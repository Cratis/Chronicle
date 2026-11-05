// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Represents an edge as stored in a graph.
/// </summary>
/// <param name="Reference">The <see cref="EdgeReference"/> identifying the edge.</param>
/// <param name="Properties">The properties of the edge.</param>
/// <param name="Watermark">The sequence number of the last event that wrote the edge.</param>
public record GraphEdge(EdgeReference Reference, IReadOnlyDictionary<string, object?> Properties, EventSequenceNumber Watermark);
