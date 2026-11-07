// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Represents a node as stored in a graph.
/// </summary>
/// <param name="Reference">The <see cref="NodeReference"/> identifying the node.</param>
/// <param name="Properties">The properties of the node.</param>
/// <param name="Watermark">The sequence number of the last event that wrote the node, or <see cref="EventSequenceNumber.Unavailable"/> when only an edge has created it.</param>
public record GraphNode(NodeReference Reference, IReadOnlyDictionary<string, object?> Properties, EventSequenceNumber Watermark);
