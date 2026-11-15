// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents a graph projection resolved to what it does for each event type.
/// </summary>
/// <param name="graph">The <see cref="GraphDefinition"/> of the graph the projection targets.</param>
/// <param name="byEventType">The <see cref="GraphEventMappings"/> per event type.</param>
public class GraphProjectionPlan(GraphDefinition graph, IReadOnlyDictionary<EventType, GraphEventMappings> byEventType)
{
    /// <summary>
    /// Gets the <see cref="GraphDefinition"/> of the graph the projection targets.
    /// </summary>
    public GraphDefinition Graph { get; } = graph;

    /// <summary>
    /// Gets the event types the projection reacts to.
    /// </summary>
    public IEnumerable<EventType> EventTypes => byEventType.Keys;

    /// <summary>
    /// Get the mappings for an event type.
    /// </summary>
    /// <param name="eventType">The <see cref="EventType"/> of the event.</param>
    /// <returns>The <see cref="GraphEventMappings"/>, or null if the projection does nothing for the event type.</returns>
    public GraphEventMappings? GetMappingsFor(EventType eventType) =>
        byEventType.TryGetValue(eventType, out var mappings) ? mappings : byEventType.FirstOrDefault(_ => _.Key.Id == eventType.Id).Value;
}
