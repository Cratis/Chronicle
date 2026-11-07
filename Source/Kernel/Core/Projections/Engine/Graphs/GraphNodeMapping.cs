// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents how an event creates or updates a node, resolved from a graph projection definition.
/// </summary>
/// <param name="Label">The node label.</param>
/// <param name="KeyProperty">The name of the key property of the node.</param>
/// <param name="Key">The <see cref="ValueProvider{T}"/> for the key of the node.</param>
/// <param name="PropertyNames">The names of the properties the mapping sets, in the order of <paramref name="Properties"/>.</param>
/// <param name="Properties">The <see cref="PropertyMapper{TSource, TTarget}"/> for each property, including counters.</param>
/// <param name="UpdateOnly">Whether the mapping only sets properties on a node that exists.</param>
/// <param name="ReadsCurrentState">Whether a property is a counter, which needs the current value of the node from the sink.</param>
public record GraphNodeMapping(
    string Label,
    string KeyProperty,
    ValueProvider<AppendedEvent> Key,
    IReadOnlyList<string> PropertyNames,
    IReadOnlyList<PropertyMapper<AppendedEvent, ExpandoObject>> Properties,
    bool UpdateOnly,
    bool ReadsCurrentState);
