// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_appending_with_named_tags(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    static readonly NamedTag[] _namedTags =
    [
        new(new TagName("account"), "one"),
        new(new TagName("account"), "two"),
        new(new TagName("region"), "north")
    ];

    BsonArray _storedNamedTags;
    AppendedEvent _readBack;

    async Task Because()
    {
        await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            "some-source",
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.NotSet,
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash> { { EventTypeGeneration.First, EventHash.NotSet } },
            null,
            _namedTags);

        var document = await _rawEvents.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        _storedNamedTags = document["namedTags"].AsBsonArray;
        _readBack = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_store_the_tag_names_in_order() => string.Join(',', _storedNamedTags.Select(_ => _["name"].AsString)).ShouldEqual("account,account,region");
    [Fact] void should_store_the_tag_values_in_order() => string.Join(',', _storedNamedTags.Select(_ => _["value"].AsString)).ShouldEqual("one,two,north");
    [Fact] void should_read_back_the_named_tags() => _readBack.Context.NamedTags.ShouldContainOnly(_namedTags);
}
