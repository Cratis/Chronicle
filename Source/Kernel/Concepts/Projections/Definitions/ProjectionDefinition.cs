// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition of a projection.
/// </summary>
/// <param name="EventSequenceId"><see cref="EventSequenceId">Event sequence identifier</see> the projection projects from.</param>
/// <param name="Identifier"><see cref="ProjectionId">Identifier</see> of the projection.</param>
/// <param name="Model">The target <see cref="ModelDefinition"/>.</param>
/// <param name="IsActive">Whether or not the projection is an actively observing projection.</param>
/// <param name="IsRewindable">Whether or not the projection is rewindable.</param>
/// <param name="InitialModelState">The initial state to use for new instances of the model.</param>
/// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Join">All the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
/// <param name="FromDerivatives">All the <see cref="Definitions.FromDerivatives"/> for an event type used as a base type in a From statement.</param>
/// <param name="FromEvery">The full <see cref="FromEveryDefinition"/>.</param>
/// <param name="Sink">The <see cref="SinkDefinition"/>.</param>
/// <param name="FromEventProperty">Optional <see cref="FromEventPropertyDefinition"/> definition.</param>
/// <param name="RemovedWith">The definition of what removes a child, if any.</param>
/// <param name="LastUpdated">The last time the projection definition was updated.</param>
public record ProjectionDefinition(
    EventSequenceId EventSequenceId,
    ProjectionId Identifier,
    ModelDefinition Model,
    bool IsActive,
    bool IsRewindable,
    JsonObject InitialModelState,
    IDictionary<EventType, FromDefinition> From,
    IDictionary<EventType, JoinDefinition> Join,
    IDictionary<PropertyPath, ChildrenDefinition> Children,
    IEnumerable<FromDerivatives> FromDerivatives,
    FromEveryDefinition FromEvery,
    SinkDefinition Sink,
    FromEventPropertyDefinition? FromEventProperty = default,
    RemovedWithDefinition? RemovedWith = default,
    DateTimeOffset? LastUpdated = default)
{
    /// <summary>
    /// Checks if the definition is empty or not. Empty meaning that there is no definition.
    /// </summary>
    public bool IsEmpty => From.Count == 0 && Children.Count == 0;
}
