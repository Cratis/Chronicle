// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>>> GetInstancesOfAnyInRange(
        IEnumerable<EventSourceId> eventSourceIds,
        IEnumerable<EventTypeId> eventTypes,
        EventSequenceNumber from,
        EventSequenceNumber upTo,
        CancellationToken cancellationToken = default)
    {
        var sources = eventSourceIds.Distinct().ToArray();
        var types = eventTypes.Distinct().ToArray();
        var result = new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>>();
        cancellationToken.ThrowIfCancellationRequested();
        if (sources.Length == 0 || types.Length == 0)
        {
            return result;
        }

        foreach (var source in sources)
        {
            var filter = Builders<Event>.Filter.Eq(e => e.EventSourceId, source) & Builders<Event>.Filter.Lte(e => e.SequenceNumber, upTo);
            var events = await ReadParentHistoryWindow(filter, types, from, upTo, cancellationToken);
            if (events.Count == 0)
            {
                continue;
            }

            var converted = new List<AppendedEvent>();
            foreach (var entry in events)
            {
                converted.Add(await converter.ToAppendedEvent(entry));
            }
            result[source] = converted;
        }
        return result;
    }

    async Task<List<Event>> ReadParentHistoryWindow(FilterDefinition<Event> filter, EventTypeId[] types, EventSequenceNumber from, EventSequenceNumber upTo, CancellationToken cancellationToken)
    {
        var predecessor = await ReadParentHistoryPredecessor(filter, types, from, cancellationToken);
        List<Event> events = predecessor is null ? [] : [predecessor];
        if (from <= upTo)
        {
            var window = await _collection.Find(filter & Builders<Event>.Filter.In(e => e.Type, types) & Builders<Event>.Filter.Gte(e => e.SequenceNumber, from))
                .SortByAscendingSequenceNumber().ToListAsync(cancellationToken);
            events.AddRange(window);
        }
        return events;
    }

    async Task<Event?> ReadParentHistoryPredecessor(FilterDefinition<Event> filter, EventTypeId[] types, EventSequenceNumber from, CancellationToken cancellationToken)
    {
        Event? predecessor = null;
        foreach (var type in types)
        {
            var candidates = await _collection.Find(filter & Builders<Event>.Filter.Eq(e => e.Type, type) & Builders<Event>.Filter.Lt(e => e.SequenceNumber, from))
                .SortByDescendingSequenceNumber().Limit(1).ToListAsync(cancellationToken);
            var candidate = candidates.FirstOrDefault();
            if (candidate is not null && (predecessor is null || candidate.SequenceNumber > predecessor.SequenceNumber))
            {
                predecessor = candidate;
            }
        }
        return predecessor;
    }
}
