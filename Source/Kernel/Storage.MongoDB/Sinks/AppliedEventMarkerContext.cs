// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the event a write is made for, when the sink records applied event markers.
/// </summary>
/// <param name="Namespace">The event store namespace of the event.</param>
/// <param name="SourceValue">The event source id of the event.</param>
/// <param name="EventTypeId">The id of the event type.</param>
/// <param name="SequenceNumber">The event sequence number of the event.</param>
/// <param name="RecordOwnSlot">Whether the write to the key document records a slot of its own.</param>
internal sealed record AppliedEventMarkerContext(string Namespace, string SourceValue, string EventTypeId, EventSequenceNumber SequenceNumber, bool RecordOwnSlot)
{
    /// <summary>
    /// Gets the identity of the event stream of the event.
    /// </summary>
    public string OwnStream => AppliedEventMarkers.StreamHash(Namespace, SourceValue);

    /// <summary>
    /// Creates the context of an event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="recordOwnSlot">Whether the write to the key document records a slot of its own.</param>
    /// <returns>The context.</returns>
    public static AppliedEventMarkerContext For(AppendedEvent @event, bool recordOwnSlot) =>
        new(@event.Context.Namespace.Value, @event.Context.EventSourceId.Value, @event.Context.EventType.Id.Value, @event.Context.SequenceNumber, recordOwnSlot);
}

/// <summary>
/// Represents the slot of a join that a changeset resolved into the key document.
/// </summary>
/// <param name="DefinitionId">The identity of the join definition.</param>
/// <param name="Stream">The identity of the event stream of the join event.</param>
/// <param name="SequenceNumber">The event sequence number of the join event.</param>
internal sealed record AppliedResolvedJoinSlot(string DefinitionId, string Stream, EventSequenceNumber SequenceNumber);

/// <summary>
/// Collects the slots of the joins a changeset resolved into the key document.
/// </summary>
internal sealed class AppliedEventMarkerTracker
{
    readonly List<AppliedResolvedJoinSlot> _slots = [];

    /// <summary>
    /// Gets the collected slots.
    /// </summary>
    public IReadOnlyList<AppliedResolvedJoinSlot> ResolvedJoins => _slots;

    /// <summary>
    /// Collects the slot of a resolved join whose source is an appended event.
    /// </summary>
    /// <param name="resolved">The resolved join.</param>
    public void Add(ResolvedJoin resolved)
    {
        if (resolved.Source is not AppendedEvent source)
        {
            return;
        }

        var definitionId = AppliedEventMarkers.DefinitionId(source.Context.EventType.Id.Value, resolved.OnProperty.Path);
        var stream = AppliedEventMarkers.StreamHash(source.Context.Namespace.Value, source.Context.EventSourceId.Value);
        _slots.Add(new(definitionId, stream, source.Context.SequenceNumber));
    }
}

/// <summary>
/// Builds the paths, values and conditions of applied event marker slots.
/// </summary>
internal static class AppliedEventMarkerSlots
{
    /// <summary>
    /// Gets the path of the slot a write to the key document records.
    /// </summary>
    /// <param name="segment">The segment of the changeset.</param>
    /// <returns>The path.</returns>
    public static string Own(string segment) => $"{WellKnownProperties.AppliedEventMarkers}.f.{segment}";

    /// <summary>
    /// Gets the path of the slot of a root join.
    /// </summary>
    /// <param name="definitionId">The identity of the join definition.</param>
    /// <returns>The path.</returns>
    public static string Join(string definitionId) => $"{WellKnownProperties.AppliedEventMarkers}.j.{definitionId}";

    /// <summary>
    /// Gets the segment a list of changes is written as: the join it starts with, or the single pass.
    /// </summary>
    /// <param name="changes">The changes of the segment.</param>
    /// <param name="context">The <see cref="AppliedEventMarkerContext"/>.</param>
    /// <returns>The segment name.</returns>
    public static string SegmentOf(IEnumerable<Change> changes, AppliedEventMarkerContext context) =>
        changes.FirstOrDefault() is Joined joined ? AppliedEventMarkers.DefinitionId(context.EventTypeId, joined.OnProperty.Path) : "0";

    /// <summary>
    /// Gets the condition that holds when a slot does not already record the event, or a later one of the same stream.
    /// </summary>
    /// <param name="slot">The path of the slot.</param>
    /// <param name="stream">The identity of the stream.</param>
    /// <param name="sequenceNumber">The event sequence number.</param>
    /// <returns>The condition.</returns>
    public static FilterDefinition<BsonDocument> NotApplied(string slot, string stream, EventSequenceNumber sequenceNumber) =>
        new BsonDocument("$nor", new BsonArray
        {
            new BsonDocument
            {
                { $"{slot}.s", stream },
                { $"{slot}.n", new BsonDocument("$gte", (long)sequenceNumber.Value) }
            }
        });

    /// <summary>
    /// Gets the value of a slot.
    /// </summary>
    /// <param name="stream">The identity of the stream.</param>
    /// <param name="sequenceNumber">The event sequence number.</param>
    /// <returns>The value.</returns>
    public static BsonDocument Value(string stream, EventSequenceNumber sequenceNumber) =>
        new() { { "s", stream }, { "n", (long)sequenceNumber.Value } };
}
