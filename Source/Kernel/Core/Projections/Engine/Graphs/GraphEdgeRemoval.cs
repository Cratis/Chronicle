// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents how an event deletes an edge, resolved from a graph projection definition.
/// </summary>
/// <param name="Type">The edge type.</param>
/// <param name="SourceLabel">The label of the source node.</param>
/// <param name="TargetLabel">The label of the target node.</param>
/// <param name="SourceKey">The <see cref="ValueProvider{T}"/> for the key of the source node.</param>
/// <param name="TargetKey">The <see cref="ValueProvider{T}"/> for the key of the target node.</param>
/// <param name="Identity">The <see cref="ValueProvider{T}"/> for the identity of the edge, if the edge type has an identifying property.</param>
public record GraphEdgeRemoval(
    string Type,
    string SourceLabel,
    string TargetLabel,
    ValueProvider<AppendedEvent> SourceKey,
    ValueProvider<AppendedEvent> TargetKey,
    ValueProvider<AppendedEvent>? Identity);
