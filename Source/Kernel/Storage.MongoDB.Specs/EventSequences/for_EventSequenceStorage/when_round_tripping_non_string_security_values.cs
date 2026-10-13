// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Specs.Compliance.given;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_round_tripping_non_string_security_values(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    readonly protected_values _values = new();
    Event _stored;
    JsonObject _released;

    void Establish()
    {
        _eventTypesStorage.HasFor(_eventType.Id, _eventType.Generation).Returns(true);
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation)
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, _values.Schema));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => _values.Converter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(call => _values.Converter.ToExpandoObject(call.Arg<JsonObject>(), call.Arg<JsonSchema>()));
    }

    async Task Because()
    {
        var encrypted = await _values.Encrypt();
        (await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            protected_values.Subject,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.NotSet,
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = encrypted },
            new Dictionary<EventTypeGeneration, EventHash>())).IsSuccess.ShouldBeTrue();
        _stored = await _collection.Find(entry => entry.SequenceNumber == EventSequenceNumber.First).SingleAsync();
        var read = await _storage.GetEventAt(EventSequenceNumber.First);
        _released = await _values.Release(read.Content);
    }

    [Fact] void should_store_ciphertext_for_every_scope_and_type() => _stored.Content["1"].Elements.All(element => element.Value.IsString).ShouldBeTrue();
    [Fact] void should_restore_every_original_json_type_and_value() => JsonNode.DeepEquals(_released, _values.Plaintext).ShouldBeTrue();
}
