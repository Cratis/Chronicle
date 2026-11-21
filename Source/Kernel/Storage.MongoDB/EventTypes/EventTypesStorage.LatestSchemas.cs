// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

/// <summary>
/// Holds the reuse of the latest parsed <see cref="EventTypeSchema"/> of every event type.
/// </summary>
public partial class EventTypesStorage
{
    /// <summary>
    /// Keeps the latest parsed <see cref="EventTypeSchema"/> per event type together with what it was parsed from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every caller that asks for the latest schemas of all event types keeps what it gets: a compiled projection holds
    /// them for as long as it is cached. Parsing a fresh set per call therefore left one full copy of every schema in
    /// the event store behind for each of them. The documents are still read on every call, and a schema is only
    /// reused while the stored generation, schema and metadata equal what it was parsed from, so a change made by
    /// another silo is picked up on the next read. Growth is bounded by the number of registered event types.
    /// </para>
    /// <para>
    /// The returned schemas are shared between callers and must be treated as read-only: a caller that needs to change
    /// one must work on its own copy. This matches how the per-generation cache in this storage already shares instances.
    /// </para>
    /// </remarks>
    readonly ConcurrentDictionary<EventTypeId, LatestSchema> _latestSchemasByType = new();

    EventTypeSchema LatestFor(EventType document)
    {
        var generation = document.GetLatestGeneration();
        if (!document.Schemas.TryGetValue(generation.ToString(), out var stored))
        {
            return document.ToKernel(generation);
        }

        if (_latestSchemasByType.TryGetValue(document.Id, out var latest) && latest.IsParsedFrom(document, generation, stored))
        {
            return latest.Schema;
        }

        var schema = document.ToKernel(generation);
        _latestSchemasByType[document.Id] = new(stored, schema);
        return schema;
    }

    sealed record LatestSchema(BsonDocument Stored, EventTypeSchema Schema)
    {
        public bool IsParsedFrom(EventType document, EventTypeGeneration generation, BsonDocument stored) =>
            Schema.Type.Generation == generation &&
            Schema.Type.Tombstone == document.Tombstone &&
            Schema.Owner == document.Owner &&
            Schema.Source == document.Source &&
            Schema.Visibility == document.Visibility &&
            Schema.Origin == (document.Origin ?? string.Empty) &&
            (ReferenceEquals(Stored, stored) || Stored.Equals(stored));
    }
}
