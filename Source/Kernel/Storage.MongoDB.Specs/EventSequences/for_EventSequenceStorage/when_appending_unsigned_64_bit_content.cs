// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

public class when_appending_unsigned_64_bit_content : given.an_event_sequence_storage
{
    readonly EventType _eventType = new("unsigned-values", 1);
    ExpandoObject _content;
    Event _stored;
    AppendedEvent _read;
    EventConverter _reader;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","$defs":{"Counter":{"type":"integer","format":"uint64"}},"properties":{
             "value":{"type":"integer","format":"uint64"},"named":{"$ref":"#/$defs/Counter"},
             "values":{"type":"array","items":{"type":"integer","format":"uint64"}},
             "small":{"type":"integer","format":"uint64"}}}
            """);
        _content = new ExpandoObject();
        var values = (IDictionary<string, object?>)_content;
        values["value"] = ulong.MaxValue;
        values["named"] = new Counter(ulong.MaxValue);
        values["values"] = new[] { ulong.MaxValue };
        values["small"] = (ulong)long.MaxValue;
        var jsonConverter = new Json.ExpandoObjectConverter(new TypeFormats());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(
            call => jsonConverter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        _eventTypesStorage.HasFor(_eventType.Id, _eventType.Generation).Returns(true);
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(
            new EventTypeSchema(_eventType, EventTypeOwner.Client, EventTypeSource.Code, schema));
        _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _collection.When(collection => collection.InsertOneAsync(Arg.Any<Event>(), Arg.Any<InsertOneOptions?>(), Arg.Any<CancellationToken>()))
            .Do(call => _stored = call.Arg<Event>());
        _reader = new EventConverter(_eventStoreName, _namespaceName, _eventTypesStorage, _identityStorage, jsonConverter);
    }

    async Task Because()
    {
        await _storage.Append(
            1,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.NotSet,
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = _content },
            new Dictionary<EventTypeGeneration, EventHash>());
        _read = await _reader.ToAppendedEvent(_stored);
    }

    [Fact] void should_store_large_unsigned_values_as_decimal128() => _stored.Content["1"]["value"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_keep_int64_for_values_within_its_range() => _stored.Content["1"]["small"].BsonType.ShouldEqual(BsonType.Int64);
    [Fact] void should_round_trip_the_scalar() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_round_trip_the_named_primitive() => ((IDictionary<string, object?>)_read.Content)["named"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_round_trip_the_array_element() => ((object[])((IDictionary<string, object?>)_read.Content)["values"]!)[0].ShouldEqual(ulong.MaxValue);

    record Counter(ulong Value) : ConceptAs<ulong>(Value);
}
