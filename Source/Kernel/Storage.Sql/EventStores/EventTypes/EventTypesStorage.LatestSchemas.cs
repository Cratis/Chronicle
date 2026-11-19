// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

/// <summary>
/// Holds the reuse of the latest parsed <see cref="EventTypeSchema"/> of every event type.
/// </summary>
public partial class EventTypesStorage
{
    /// <summary>
    /// Keeps the latest parsed <see cref="EventTypeSchema"/> per event type together with what it was parsed from.
    /// </summary>
    /// <remarks>
    /// Every caller that asks for the latest schemas of all event types keeps what it gets: a compiled projection holds
    /// them for as long as it is cached. Parsing a fresh set per call therefore left one full copy of every schema in
    /// the event store behind for each of them. The rows are still read on every call, and a schema is only reused
    /// while the stored generation, schema and metadata equal what it was parsed from, so a change made by another
    /// silo is picked up on the next read. Growth is bounded by the number of registered event types.
    /// </remarks>
    readonly ConcurrentDictionary<EventTypeId, LatestSchema> _latestSchemasByType = new();

    EventTypeSchema LatestFor(EventType row)
    {
        if (row.Schemas.Count == 0)
        {
            return row.ToKernelForLatestGeneration();
        }

        var generation = new EventTypeGeneration(row.Schemas.Keys.Max());
        var stored = row.Schemas[(uint)generation];
        if (_latestSchemasByType.TryGetValue(row.Id, out var latest) && latest.IsParsedFrom(row, generation, stored))
        {
            return latest.Schema;
        }

        var schema = row.ToKernel(generation);
        _latestSchemasByType[row.Id] = new(stored, schema);
        return schema;
    }

    sealed record LatestSchema(string Stored, EventTypeSchema Schema)
    {
        public bool IsParsedFrom(EventType row, EventTypeGeneration generation, string stored) =>
            Schema.Type.Generation == generation &&
            Schema.Type.Tombstone == row.Tombstone &&
            Schema.Owner == row.Owner &&
            Schema.Source == row.Source &&
            Schema.Visibility == row.Visibility &&
            Schema.Origin == (row.Origin ?? string.Empty) &&
            string.Equals(Stored, stored, StringComparison.Ordinal);
    }
}
