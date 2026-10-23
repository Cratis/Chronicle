// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>>> GetLastInstancesOfAny(IEnumerable<EventSourceId> eventSourceIds, IEnumerable<EventTypeId> eventTypes)
    {
        var types = eventTypes.ToArray();
        var results = eventSourceIds.Distinct().ToDictionary(source => source, _ => Option<AppendedEvent>.None());
        foreach (var sources in results.Keys.ToArray().Chunk(1000))
        {
            var filter = Builders<Event>.Filter.And(
                Builders<Event>.Filter.In(@event => @event.EventSourceId, sources),
                Builders<Event>.Filter.In(@event => @event.Type, types));
            var latest = await ReadLastInstances(filter);
            foreach (var @event in latest)
            {
                results[@event.EventSourceId] = await converter.ToAppendedEvent(@event);
            }
        }

        return results;
    }

    async Task<IEnumerable<Event>> ReadLastInstances(FilterDefinition<Event> filter)
    {
        // Equality on both eventSourceId and type is bounded by the eventSourceId_eventTypeId index, so only the page's
        // matching events are read. The latest per source is picked here, so no server-side sort or grouping is needed.
        using var cursor = await _collection.Find(filter, new FindOptions { BatchSize = 1000 }).ToCursorAsync();
        var latest = new Dictionary<EventSourceId, Event>();
        while (await cursor.MoveNextAsync())
        {
            foreach (var @event in cursor.Current)
            {
                if (!latest.TryGetValue(@event.EventSourceId, out var previous) || @event.SequenceNumber > previous.SequenceNumber)
                {
                    latest[@event.EventSourceId] = @event;
                }
            }
        }

        return latest.Values;
    }
}
