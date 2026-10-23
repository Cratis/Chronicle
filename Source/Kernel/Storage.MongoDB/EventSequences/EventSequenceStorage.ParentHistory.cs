// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
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

        foreach (var chunk in sources.Chunk(512))
        {
            var filter = Builders<Event>.Filter.In(e => e.EventSourceId, chunk) &
                Builders<Event>.Filter.In(e => e.Type, types) & Builders<Event>.Filter.Lte(e => e.SequenceNumber, upTo);
            var events = await ReadParentHistoryWindow(filter, from, upTo, chunk.Length, cancellationToken);
            foreach (var group in events.GroupBy(e => e.EventSourceId))
            {
                var converted = new List<AppendedEvent>();
                foreach (var entry in group)
                {
                    converted.Add(await converter.ToAppendedEvent(entry));
                }
                result[group.Key] = converted;
            }
        }
        return result;
    }

    async Task<List<Event>> ReadParentHistoryWindow(FilterDefinition<Event> filter, EventSequenceNumber from, EventSequenceNumber upTo, int sourceCount, CancellationToken cancellationToken)
    {
        if (sourceCount == 1 && from > upTo)
        {
            // Live and on-demand predecessor lookups read just one indexed candidate.
            return await _collection.Find(filter).SortByDescendingSequenceNumber().Limit(1).ToListAsync(cancellationToken);
        }

        var predecessors = await _collection.Aggregate()
            .Match(filter & Builders<Event>.Filter.Lt(e => e.SequenceNumber, from))
            .Sort(Builders<Event>.Sort.Ascending(e => e.EventSourceId).Descending(e => e.SequenceNumber))
            .Group(new BsonDocument { { "_id", "$eventSourceId" }, { "sequenceNumber", new BsonDocument("$first", "$_id") } })
            .ToListAsync(cancellationToken);
        var locators = predecessors.Select(e => ToEventSequenceNumber(e["sequenceNumber"])).ToArray();
        var window = Builders<Event>.Filter.Gte(e => e.SequenceNumber, from) | Builders<Event>.Filter.In(e => e.SequenceNumber, locators);
        return await _collection.Find(filter & window).SortByAscendingSequenceNumber().ToListAsync(cancellationToken);
    }
}
