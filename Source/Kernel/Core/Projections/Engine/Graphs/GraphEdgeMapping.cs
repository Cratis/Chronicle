// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents how an event creates or updates an edge, resolved from a graph projection definition.
/// </summary>
/// <param name="Type">The edge type.</param>
/// <param name="SourceLabel">The label of the source node.</param>
/// <param name="TargetLabel">The label of the target node.</param>
/// <param name="SourceKey">The <see cref="ValueProvider{T}"/> for the key of the source node.</param>
/// <param name="TargetKey">The <see cref="ValueProvider{T}"/> for the key of the target node.</param>
/// <param name="IdentifyingProperty">The name of the identifying property of the edge type, if it has one.</param>
/// <param name="Identity">The <see cref="ValueProvider{T}"/> for the identity of the edge, if the edge type has an identifying property.</param>
/// <param name="PropertyNames">The names of the properties the mapping sets, in the order of <paramref name="Properties"/>.</param>
/// <param name="Properties">The <see cref="PropertyMapper{TSource, TTarget}"/> for each property, including counters.</param>
/// <param name="ReadsCurrentState">Whether a property is a counter, which needs the current value of the edge from the sink.</param>
public record GraphEdgeMapping(
    string Type,
    string SourceLabel,
    string TargetLabel,
    ValueProvider<AppendedEvent> SourceKey,
    ValueProvider<AppendedEvent> TargetKey,
    string? IdentifyingProperty,
    ValueProvider<AppendedEvent>? Identity,
    IReadOnlyList<string> PropertyNames,
    IReadOnlyList<PropertyMapper<AppendedEvent, ExpandoObject>> Properties,
    bool ReadsCurrentState);
