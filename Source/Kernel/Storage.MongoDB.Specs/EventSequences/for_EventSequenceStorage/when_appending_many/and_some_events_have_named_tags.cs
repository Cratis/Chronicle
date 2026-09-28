// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_some_events_have_named_tags(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    static readonly NamedTag[] _firstEventTags = [new(new TagName("account"), "one"), new(new TagName("region"), "north")];
    static readonly NamedTag[] _thirdEventTags = [new(new TagName("account"), "three")];

    List<BsonDocument> _documents;
    AppendedEvent _firstReadBack;
    AppendedEvent _secondReadBack;
    AppendedEvent _thirdReadBack;

    async Task Because()
    {
        await _storage.AppendManyWithNamedTags(
        [
            EventAt(EventSequenceNumber.First) with { NamedTags = _firstEventTags },
            EventAt(EventSequenceNumber.First + 1),
            EventAt(EventSequenceNumber.First + 2) with { NamedTags = _thirdEventTags }
        ]);

        _documents = await _rawEvents.Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Ascending("_id")).ToListAsync();
        _firstReadBack = await _storage.GetEventAt(EventSequenceNumber.First);
        _secondReadBack = await _storage.GetEventAt(EventSequenceNumber.First + 1);
        _thirdReadBack = await _storage.GetEventAt(EventSequenceNumber.First + 2);
    }

    [Fact] void should_store_every_event() => _documents.Count.ShouldEqual(3);
    [Fact] void should_store_the_tag_names_of_the_first_event() => NamesOf(_documents[0]).ShouldEqual("account,region");
    [Fact] void should_store_the_tag_values_of_the_first_event() => ValuesOf(_documents[0]).ShouldEqual("one,north");
    [Fact] void should_store_no_named_tags_for_the_untagged_event() => _documents[1]["namedTags"].AsBsonArray.ShouldBeEmpty();
    [Fact] void should_store_the_tag_names_of_the_third_event() => NamesOf(_documents[2]).ShouldEqual("account");
    [Fact] void should_store_the_tag_values_of_the_third_event() => ValuesOf(_documents[2]).ShouldEqual("three");
    [Fact] void should_read_back_the_named_tags_of_the_first_event() => _firstReadBack.Context.NamedTags.ShouldContainOnly(_firstEventTags);
    [Fact] void should_read_back_no_named_tags_for_the_untagged_event() => _secondReadBack.Context.NamedTags.ShouldBeEmpty();
    [Fact] void should_read_back_the_named_tags_of_the_third_event() => _thirdReadBack.Context.NamedTags.ShouldContainOnly(_thirdEventTags);

    static string NamesOf(BsonDocument document) => string.Join(',', document["namedTags"].AsBsonArray.Select(_ => _["name"].AsString));
    static string ValuesOf(BsonDocument document) => string.Join(',', document["namedTags"].AsBsonArray.Select(_ => _["value"].AsString));
}
