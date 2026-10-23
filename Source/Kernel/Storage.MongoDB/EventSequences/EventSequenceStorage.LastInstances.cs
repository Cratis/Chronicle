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
            using var cursor = await _collection.Aggregate()
                .Match(filter)
                .Sort(Builders<Event>.Sort.Descending(@event => @event.SequenceNumber))
                .Group(@event => @event.EventSourceId, group => group.First())
                .ToCursorAsync();
            while (await cursor.MoveNextAsync())
            {
                foreach (var @event in cursor.Current)
                {
                    results[@event.EventSourceId] = await converter.ToAppendedEvent(@event);
                }
            }
        }

        return results;
    }
}
