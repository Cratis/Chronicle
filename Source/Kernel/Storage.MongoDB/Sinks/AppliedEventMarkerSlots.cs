// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

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
