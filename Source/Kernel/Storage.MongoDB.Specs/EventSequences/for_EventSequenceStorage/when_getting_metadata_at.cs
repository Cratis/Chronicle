// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(MongoDBCollection.Name)]
public class when_getting_metadata_at(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    EventSequenceStorage _storage;
    IEventConverter _converter;
    IdentityId _identityId;
    IReadOnlyList<StoredEventMetadata> _before;
    IReadOnlyList<StoredEventMetadata> _after;
    IReadOnlyList<StoredEventMetadata> _sorted;

    async Task Establish()
    {
        _identityId = IdentityId.New();
        _converter = Substitute.For<IEventConverter>();
        _storage = new EventSequenceStorage(_eventStore, _namespace, EventSequenceId.Log, _database, _converter, Substitute.For<IEventTypesStorage>(), Substitute.For<IIdentityStorage>(), Substitute.For<Json.IExpandoObjectConverter>(), JsonSerializerOptions.Default, Substitute.For<ILogger<EventSequenceStorage>>());
        var collection = _database.GetEventSequenceCollectionFor(EventSequenceId.Log);
        var document = AnEvent(1UL).ToBsonDocument();
        var contentField = BsonClassMap.LookupClassMap(typeof(Event)).GetMemberMap(nameof(Event.Content)).ElementName;

        // This cannot deserialize as event content. A metadata projection must exclude it completely.
        document[contentField] = new BsonString("invalid-content");
        await _rawDatabase.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName).InsertOneAsync(document);
    }

    async Task Because()
    {
        _before = await _storage.GetMetadataAt([1UL, 100000UL, 1UL]);
        await _database.GetEventSequenceCollectionFor(EventSequenceId.Log).InsertManyAsync(Enumerable.Range(2, 10000).Select(_ => AnEvent((ulong)_)));
        _after = await _storage.GetMetadataAt([1UL, 100000UL]);
        _sorted = await _storage.GetMetadataAt([10001UL, 1UL, 5000UL, 1UL, 100000UL]);
    }

    [Fact] void should_read_metadata_without_deserializing_content() => _before.Count.ShouldEqual(1);
    [Fact] void should_stay_bounded_after_ten_thousand_more_events() => _after.Count.ShouldEqual(1);
    [Fact] void should_return_metadata_in_sequence_order() => _sorted.Select(_ => _.SequenceNumber.Value).SequenceEqual([1UL, 5000UL, 10001UL]).ShouldBeTrue();
    [Fact] void should_preserve_the_identity_chain() => _after.Single().CausedByChain.ShouldContainOnly(_identityId);
    [Fact] void should_not_resolve_or_release_event_content() => _converter.ReceivedCalls().ShouldBeEmpty();

    Event AnEvent(ulong number) => new(number, CorrelationId.New(), [], [_identityId], "type", DateTimeOffset.UtcNow, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, [], new Dictionary<string, BsonDocument>(), new Dictionary<string, string>(), []);
}
