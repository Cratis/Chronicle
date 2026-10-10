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

public class when_appending_decimal_content : given.an_event_sequence_storage
{
    readonly EventType _eventType = new("decimal-values", 1);
    ExpandoObject _content;
    Event _stored;
    AppendedEvent _read;
    EventConverter _reader;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"},"precise":{"type":"number","format":"decimal"}}}""");
        dynamic content = new ExpandoObject();
        content.amount = 193.58m;
        content.precise = 1234567890.123456789012345678m;
        _content = content;
        var converter = new Json.ExpandoObjectConverter(new TypeFormats());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(
            call => converter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        _eventTypesStorage.HasFor(_eventType.Id, _eventType.Generation).Returns(true);
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(new EventTypeSchema(_eventType, EventTypeOwner.Client, EventTypeSource.Code, schema));
        _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _collection.When(collection => collection.InsertOneAsync(Arg.Any<Event>(), Arg.Any<InsertOneOptions?>(), Arg.Any<CancellationToken>())).Do(call => _stored = call.Arg<Event>());
        _reader = new EventConverter(_eventStoreName, _namespaceName, _eventTypesStorage, _identityStorage, converter);
    }

    async Task Because()
    {
        await _storage.Append(1, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = _content }, new Dictionary<EventTypeGeneration, EventHash>());
        _read = await _reader.ToAppendedEvent(_stored);
    }

    [Fact] void should_store_decimal128() => _stored.Content["1"]["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)((IDictionary<string, object?>)_read.Content)["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)((IDictionary<string, object?>)_read.Content)["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
