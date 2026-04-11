// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

public class when_appending_event_with_multiple_generations : given.an_event_sequence_storage
{
    EventSequenceNumber _sequenceNumber;
    EventType _eventType;
    IDictionary<EventTypeGeneration, ExpandoObject> _content;
    IDictionary<EventTypeGeneration, EventHash> _contentHashes;
    Result<AppendedEvent, DuplicateEventSequenceNumber> _result;
    JsonSchema _gen1Schema;
    JsonSchema _gen2Schema;
    Identity _identity;
    Event _insertedEvent;

    async Task Establish()
    {
        _sequenceNumber = 1;
        _eventType = new EventType(Guid.NewGuid().ToString(), 1);

        _gen1Schema = await JsonSchema.FromJsonAsync("{}");
        _gen2Schema = await JsonSchema.FromJsonAsync("{}");

        _eventTypesStorage.GetFor(_eventType.Id, (EventTypeGeneration)1)
            .Returns(new EventTypeSchema(_eventType with { Generation = 1 }, EventTypeOwner.None, EventTypeSource.Unknown, _gen1Schema));
        _eventTypesStorage.GetFor(_eventType.Id, (EventTypeGeneration)2)
            .Returns(new EventTypeSchema(_eventType with { Generation = 2 }, EventTypeOwner.None, EventTypeSource.Unknown, _gen2Schema));

        var gen1Expando = new ExpandoObject();
        dynamic gen1Dynamic = gen1Expando;
        gen1Dynamic.name = "John Doe";

        var gen2Expando = new ExpandoObject();
        dynamic gen2Dynamic = gen2Expando;
        gen2Dynamic.firstName = "John";
        gen2Dynamic.lastName = "Doe";

        _content = new Dictionary<EventTypeGeneration, ExpandoObject>
        {
            [(EventTypeGeneration)1] = gen1Expando,
            [(EventTypeGeneration)2] = gen2Expando
        };

        _contentHashes = new Dictionary<EventTypeGeneration, EventHash>
        {
            [(EventTypeGeneration)1] = "hash-gen-1",
            [(EventTypeGeneration)2] = "hash-gen-2"
        };

        _expandoObjectConverter.ToJsonObject(Arg.Is<ExpandoObject>(e => e == gen1Expando), Arg.Any<JsonSchema>())
            .Returns(new JsonObject { ["name"] = "John Doe" });
        _expandoObjectConverter.ToJsonObject(Arg.Is<ExpandoObject>(e => e == gen2Expando), Arg.Any<JsonSchema>())
            .Returns(new JsonObject { ["firstName"] = "John", ["lastName"] = "Doe" });

        _identity = Identity.System;
        _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>())
            .Returns(_identity);

        _collection
            .When(_ => _.InsertOneAsync(Arg.Any<Event>(), Arg.Any<InsertOneOptions?>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => _insertedEvent = callInfo.Arg<Event>());
    }

    async Task Because() => _result = await _storage.Append(
        _sequenceNumber,
        EventSourceType.Default,
        "some-event-source",
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        CorrelationId.New(),
        [],
        [IdentityId.NotSet],
        [],
        DateTimeOffset.UtcNow,
        _content,
        _contentHashes);

    [Fact] void should_attempt_to_insert_event() =>
        _collection.Received(1).InsertOneAsync(Arg.Any<Event>(), Arg.Any<InsertOneOptions?>(), Arg.Any<CancellationToken>());
    [Fact] void should_have_content_hash_for_generation_1() => _insertedEvent.ContentHashes["1"].ShouldEqual("hash-gen-1");
    [Fact] void should_have_content_hash_for_generation_2() => _insertedEvent.ContentHashes["2"].ShouldEqual("hash-gen-2");
    [Fact] void should_have_hash_on_appended_event_context() => ((AppendedEvent)_result).Context.Hash.ShouldNotEqual(EventHash.NotSet);
}
