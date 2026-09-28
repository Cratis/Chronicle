// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Driver;

using Tag = Cratis.Chronicle.Concepts.Events.Tag;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public class a_replica_set_storage_with_named_tags(ReplicaSetMongoDBFixture fixture) : a_replica_set_event_sequence_storage(fixture)
{
    protected static readonly DateTimeOffset _firstDay = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset _secondDay = _firstDay.AddDays(1);
    protected BsonDocument _historicDocument;

    async Task Establish()
    {
        var otherEventType = new EventType("other-event", EventTypeGeneration.First);
        await _storage.AppendManyWithNamedTags(
        [
        EventAt(EventSequenceNumber.First) with
        {
            Occurred = _firstDay,
            Tags = [new Tag("important")],
            NamedTags = [new(new TagName("account"), "one"), new(new TagName("region"), "north")]
        },
        EventAt(EventSequenceNumber.First + 1) with
        {
            Occurred = _firstDay,
            NamedTags = [new(new TagName("account"), "two"), new(new TagName("region"), "one")]
        },
        EventAt(EventSequenceNumber.First + 2, otherEventType) with
        {
            Occurred = _firstDay,
            Tags = [new Tag("important")],
            NamedTags = [new(new TagName("account"), "three"), new(new TagName("region"), "south")]
        },
        EventAt(EventSequenceNumber.First + 3) with
        {
            Occurred = _secondDay,
            EventSourceId = "other-source",
            Tags = [new Tag("ordinary")],
            NamedTags = [new(new TagName("account"), "one")]
        },
        EventAt(EventSequenceNumber.First + 4) with
        {
            Occurred = _secondDay,
            EventSourceId = "historic-source",
            Tags = [new Tag("important")]
        }
        ]);

        var historicFilter = Builders<BsonDocument>.Filter.Eq("eventSourceId", "historic-source");
        await _rawEvents.UpdateOneAsync(historicFilter, Builders<BsonDocument>.Update.Unset("namedTags"));
        _historicDocument = await _rawEvents.Find(historicFilter).SingleAsync();
    }
}
