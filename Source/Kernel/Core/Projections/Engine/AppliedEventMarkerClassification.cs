// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Decides which root projections are marked for <see cref="SinkWriteMode.OncePerSourceEvent"/>, and which join definitions
/// the sink keeps applied event markers for.
/// </summary>
/// <remarks>
/// A root projection is marked when, and only when: it has at least one root join and every root join is keyed by the event
/// source id; it has no children, no nested objects and does not subscribe to every event; every event type that is not
/// join-only resolves its key to the event source id; and it is not globally scoped, because a global projection shares one
/// destination between namespaces and equal event source ids from two namespaces would alternate the marker slot. This is
/// <see cref="IProjection.IsEventSourceKeyed"/> with join-only event types ignored.
/// </remarks>
internal static class AppliedEventMarkerClassification
{
    /// <summary>
    /// Classifies a root projection.
    /// </summary>
    /// <param name="scope">The <see cref="ProjectionScope"/> of the projection.</param>
    /// <param name="subscribesToAllEvents">Whether the projection subscribes to every event.</param>
    /// <param name="hasChildren">Whether the projection has child projections.</param>
    /// <param name="hasNested">Whether the projection has nested objects.</param>
    /// <param name="joins">The root joins of the projection.</param>
    /// <param name="eventTypes">The event types with key resolvers of the projection.</param>
    /// <param name="operationTypes">The operation types per event type.</param>
    /// <returns>The join definitions to mark, empty when the projection is not marked.</returns>
    public static IReadOnlyList<AppliedEventMarkerJoin> For(
        ProjectionScope scope,
        bool subscribesToAllEvents,
        bool hasChildren,
        bool hasNested,
        IEnumerable<KeyValuePair<EventType, JoinDefinition>> joins,
        IEnumerable<EventTypeWithKeyResolver> eventTypes,
        IDictionary<EventType, ProjectionOperationType> operationTypes)
    {
        var joinList = joins.ToArray();
        if (joinList.Length == 0 ||
            scope != ProjectionScope.Namespaced ||
            subscribesToAllEvents ||
            hasChildren ||
            hasNested ||
            joinList.Any(join => !IsKeyedByEventSourceId(join.Value.Key)) ||
            eventTypes.Any(_ => !IsJoinOnly(_.EventType, operationTypes) && !_.ResolvesToEventSourceId))
        {
            return [];
        }

        return joinList
            .Select(join => new AppliedEventMarkerJoin(
                AppliedEventMarkers.DefinitionId(join.Key.Id.Value, join.Value.On.Path),
                join.Key.Id.Value,
                join.Value.On.Path))
            .ToArray();
    }

    static bool IsKeyedByEventSourceId(PropertyExpression key) =>
        string.IsNullOrEmpty(key?.Value) || key.Value == WellKnownExpressions.EventSourceId;

    static bool IsJoinOnly(EventType eventType, IDictionary<EventType, ProjectionOperationType> operationTypes) =>
        operationTypes.TryGetValue(eventType, out var operation) &&
        operation.HasFlag(ProjectionOperationType.Join) &&
        !operation.HasFlag(ProjectionOperationType.From);
}
