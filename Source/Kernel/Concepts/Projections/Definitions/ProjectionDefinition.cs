// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition of a projection.
/// </summary>
/// <param name="Owner"><see cref="ProjectionOwner">Owner</see> of the projection.</param>
/// <param name="EventSequenceId"><see cref="EventSequenceId">Event sequence identifier</see> the projection projects from.</param>
/// <param name="Identifier"><see cref="ProjectionId">Identifier</see> of the projection.</param>
/// <param name="ReadModel">The target read model.</param>
/// <param name="IsActive">Whether or not the projection is an actively observing projection.</param>
/// <param name="IsRewindable">Whether or not the projection is rewindable.</param>
/// <param name="InitialModelState">The initial state to use for new instances of the model.</param>
/// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Join">All the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
/// <param name="FromDerivatives">All the <see cref="Definitions.FromDerivatives"/> for an event type used as a base type in a From statement.</param>
/// <param name="FromEvery">The full <see cref="FromEveryDefinition"/>.</param>
/// <param name="RemovedWith">All the <see cref="RemovedWithDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="RemovedWithJoin">All the <see cref="RemovedWithJoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="FromEventProperty">Optional <see cref="FromEventPropertyDefinition"/> definition.</param>
/// <param name="LastUpdated">The last time the projection definition was updated.</param>
/// <param name="Tags">Collection of tags the projection belongs to.</param>
/// <param name="AutoMap">Whether properties should be auto-mapped from events at the projection level.</param>
/// <param name="Nested">All the <see cref="ChildrenDefinition"/> for nested single-object properties on the model.</param>
/// <param name="SubscribesToAllEvents">Whether the projection subscribes to all event types in the system.</param>
/// <param name="NoAutoMapProperties">Read model properties excluded from auto-mapping even when <paramref name="AutoMap"/> is enabled.</param>
public record ProjectionDefinition(
    ProjectionOwner Owner,
    EventSequenceId EventSequenceId,
    ProjectionId Identifier,
    ReadModelIdentifier ReadModel,
    bool IsActive,
    bool IsRewindable,
    JsonObject InitialModelState,
    IDictionary<EventType, FromDefinition> From,
    IDictionary<EventType, JoinDefinition> Join,
    IDictionary<PropertyPath, ChildrenDefinition> Children,
    IEnumerable<FromDerivatives> FromDerivatives,
    FromEveryDefinition FromEvery,
    IDictionary<EventType, RemovedWithDefinition> RemovedWith,
    IDictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin,
    FromEventPropertyDefinition? FromEventProperty = default,
    DateTimeOffset? LastUpdated = default,
    IEnumerable<string>? Tags = default,
    AutoMap AutoMap = AutoMap.Enabled,
    IDictionary<PropertyPath, ChildrenDefinition>? Nested = default,
    bool SubscribesToAllEvents = false,
    IEnumerable<PropertyPath>? NoAutoMapProperties = default)
{
    /// <summary>
    /// Checks if the definition is empty or not. Empty meaning that there is no definition.
    /// </summary>
    /// <remarks>
    /// A grain's <see cref="ProjectionDefinition"/> state is left uninitialized (all reference members null)
    /// when nothing has been persisted for it yet - <see cref="From"/> and <see cref="Children"/> can be null
    /// in that state despite their non-nullable type, so this must not dereference them directly. It is also
    /// excluded from JSON via <see cref="JsonIgnoreAttribute"/> - it is write-only (no setter, nothing reads it
    /// back through JSON) and evaluating it during Orleans' deep-copy serialization of an uninitialized instance
    /// would otherwise throw a NullReferenceException from inside the serializer, with no Chronicle frame on the
    /// stack to explain it (#3934).
    /// </remarks>
    [JsonIgnore]
    public bool IsEmpty => (From?.Count ?? 0) == 0 && (Children?.Count ?? 0) == 0 && (Nested is null || Nested.Count == 0);
}
