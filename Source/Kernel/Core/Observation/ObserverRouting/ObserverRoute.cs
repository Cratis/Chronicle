// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Represents what the routing table knows of one observer: the definition axis, shared by every namespace.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> of the observer.</param>
/// <param name="Type">The <see cref="ObserverType"/> of the observer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the observer observes.</param>
/// <param name="EventTypes">The shared <see cref="EventTypeSet"/> the observer observes; empty when it observes every event.</param>
/// <param name="Revision">The <see cref="ObserverDefinitionRevision"/> of the definition the route was built from.</param>
/// <remarks>
/// A route carries no filters: filters are not part of a definition, so a route matches every event of its types and the
/// observer still applies its own filters. That errs toward delivery.
/// </remarks>
public sealed record ObserverRoute(
    ObserverId ObserverId,
    ObserverType Type,
    EventSequenceId EventSequenceId,
    EventTypeSet EventTypes,
    ObserverDefinitionRevision Revision)
{
    /// <summary>
    /// Gets whether the observer observes every event, whatever its type.
    /// </summary>
    public bool AllEventTypes => EventTypes.Count == 0;

    /// <summary>
    /// Creates a route from a definition.
    /// </summary>
    /// <param name="definition">The <see cref="ObserverDefinition"/> to create it from.</param>
    /// <returns>The <see cref="ObserverRoute"/>.</returns>
    public static ObserverRoute From(ObserverDefinition definition) => new(
        definition.Identifier,
        definition.Type,
        definition.EventSequenceId,
        definition.EventTypes as EventTypeSet ?? EventTypeSet.From(definition.EventTypes),
        definition.Revision);

    /// <summary>
    /// Gets whether the route takes events of a type.
    /// </summary>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to check.</param>
    /// <returns>True when it does; otherwise false.</returns>
    public bool Takes(EventTypeId eventTypeId) => AllEventTypes || EventTypes.Ids.Contains(eventTypeId);
}
