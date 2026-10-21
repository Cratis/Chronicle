// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_adding_generations;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_content_contains_decimals(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    StoredEventGenerations _observed;
    GenerationToAdd _addition;
    Event _stored;
    bool _added;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"},"precise":{"type":"number","format":"decimal"}}}""");
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Client, EventTypeSource.Code, schema));
        var converter = new Json.ExpandoObjectConverter(new TypeFormats());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => converter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        dynamic content = new ExpandoObject();
        content.amount = 193.58m;
        content.precise = 1234567890.123456789012345678m;
        var entry = EventAt(0) with { GenerationalContent = new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = content } };
        (await _storage.AppendMany([entry])).IsSuccess.ShouldBeTrue();
        _observed = (await _storage.GetStoredGenerations(0))!;
        _addition = new(2, (ExpandoObject)content, "added-hash", new(1, true, new("migration-version")));
    }

    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [_addition]);
        _stored = await _collection.Find(_ => _.SequenceNumber == 0).SingleAsync();
    }

    [Fact] void should_match_the_observed_decimal_content() => _added.ShouldBeTrue();
    [Fact] void should_leave_the_original_amount_as_decimal128() => _stored.Content["1"]["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_added_amount_as_decimal128() => _stored.Content["2"]["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_the_added_amount_bits() => decimal.GetBits(_stored.Content["2"]["amount"].AsDecimal).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => _stored.Content["2"]["precise"].AsDecimal.ShouldEqual(1234567890.123456789012345678m);
}
