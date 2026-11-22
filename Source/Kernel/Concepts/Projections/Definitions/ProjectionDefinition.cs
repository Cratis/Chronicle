// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
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
/// <param name="Scope">The <see cref="ProjectionScope"/> the projection materializes its read model in.</param>
[Immutable]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition")]
public record ProjectionDefinition(
    [Id(0)] ProjectionOwner Owner,
    [Id(1)] EventSequenceId EventSequenceId,
    [Id(2)] ProjectionId Identifier,
    [Id(3)] ReadModelIdentifier ReadModel,
    [Id(4)] bool IsActive,
    [Id(5)] bool IsRewindable,
    [Id(6)] ImmutableJsonObject InitialModelState,
    IReadOnlyDictionary<EventType, FromDefinition> From,
    IReadOnlyDictionary<EventType, JoinDefinition> Join,
    IReadOnlyDictionary<PropertyPath, ChildrenDefinition> Children,
    IEnumerable<FromDerivatives> FromDerivatives,
    [Id(11)] FromEveryDefinition FromEvery,
    IReadOnlyDictionary<EventType, RemovedWithDefinition> RemovedWith,
    IReadOnlyDictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin,
    [Id(14)] FromEventPropertyDefinition? FromEventProperty = default,
    [Id(15)] DateTimeOffset? LastUpdated = default,
    IEnumerable<string>? Tags = default,
    [Id(17)] AutoMap AutoMap = AutoMap.Enabled,
    IReadOnlyDictionary<PropertyPath, ChildrenDefinition>? Nested = default,
    [Id(19)] bool SubscribesToAllEvents = false,
    IEnumerable<PropertyPath>? NoAutoMapProperties = default,
    [Id(21)] ProjectionScope Scope = ProjectionScope.Namespaced)
{
    /// <summary>
    /// Gets all the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [Id(7)]
    public IReadOnlyDictionary<EventType, FromDefinition> From { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(From);

    /// <summary>
    /// Gets all the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [Id(8)]
    public IReadOnlyDictionary<EventType, JoinDefinition> Join { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(Join);

    /// <summary>
    /// Gets all the <see cref="ChildrenDefinition"/> for properties on model.
    /// </summary>
    [Id(9)]
    public IReadOnlyDictionary<PropertyPath, ChildrenDefinition> Children { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(Children);

    /// <summary>
    /// Gets all the <see cref="RemovedWithDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [Id(12)]
    public IReadOnlyDictionary<EventType, RemovedWithDefinition> RemovedWith { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(RemovedWith);

    /// <summary>
    /// Gets all the <see cref="RemovedWithJoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [Id(13)]
    public IReadOnlyDictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(RemovedWithJoin);

    /// <summary>
    /// Gets all the <see cref="ChildrenDefinition"/> for nested single-object properties on the model.
    /// </summary>
    [Id(18)]
    public IReadOnlyDictionary<PropertyPath, ChildrenDefinition>? Nested { get; init => field = ImmutableDefinitions.SnapshotOrNull(value); } = ImmutableDefinitions.SnapshotOrNull(Nested);

    /// <summary>
    /// Gets all the <see cref="Definitions.FromDerivatives"/> for an event type used as a base type in a From statement.
    /// </summary>
    [Id(10)]
    public IEnumerable<FromDerivatives> FromDerivatives { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(FromDerivatives);

    /// <summary>
    /// Gets collection of tags the projection belongs to.
    /// </summary>
    [Id(16)]
    public IEnumerable<string>? Tags { get; init => field = ImmutableDefinitions.SnapshotOrNull(value); } = ImmutableDefinitions.SnapshotOrNull(Tags);

    /// <summary>
    /// Gets read model properties excluded from auto-mapping even when <see cref="AutoMap"/> is enabled.
    /// </summary>
    [Id(20)]
    public IEnumerable<PropertyPath>? NoAutoMapProperties { get; init => field = ImmutableDefinitions.SnapshotOrNull(value); } = ImmutableDefinitions.SnapshotOrNull(NoAutoMapProperties);

    /// <summary>
    /// Gets the event generation delivery policy. Compatibility preserves identifier-only dispatch.
    /// </summary>
    [Id(22)]
    public EventGenerationDelivery GenerationDelivery { get; init; }

    /// <summary>
    /// Gets the graph mappings when the projection targets a graph instead of a document or relational read model.
    /// </summary>
    /// <remarks>
    /// Additive: a definition without graph mappings behaves exactly as before.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Id(23)]
    public Graphs.GraphProjectionDefinition? Graph { get; init; }

    /// <summary>
    /// Gets the software version of the client application that registered the definition, if it identified one.
    /// </summary>
    /// <remarks>
    /// Metadata, not structure: it is ignored when comparing definitions, so a version change alone never evolves
    /// the projection. It exists so a registration from an older version can be told apart from a newer one.
    /// </remarks>
    [Id(24)]
    public string? Version { get; init; }

    /// <summary>
    /// Gets whether storage predates preservation of <see cref="Scope"/> and every-event keys and AutoMap settings.
    /// </summary>
    /// <remarks>
    /// Storage metadata, not a projection rule. Those fields are unknown in legacy MongoDB definitions,
    /// including every-event settings in children and nested projections. A fresh registration supplies them
    /// without evolution; the next lossless save removes this uncertainty.
    /// </remarks>
    [Id(25)]
    public bool HasLegacyStorageFields { get; init; }

    /// <summary>
    /// Gets a value indicating whether the projection is owned by the kernel itself.
    /// </summary>
    /// <remarks>
    /// A system projection is declared in the kernel, never registered by a client, and never replayable. Every
    /// guard that has to refuse an operation on kernel-owned state asks this rather than comparing the identifier
    /// prefix, so the prefix stays a naming convention rather than a load-bearing parse.
    /// </remarks>
    [JsonIgnore]
    public bool IsKernelOwned => Owner == ProjectionOwner.Kernel;

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
    public bool IsEmpty => (From?.Count ?? 0) == 0 && (Children?.Count ?? 0) == 0 && (Nested is null || Nested.Count == 0) && Graph?.IsEmpty != false;
}
