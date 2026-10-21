// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Specs.Compliance.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_round_tripping_non_string_security_values : given.an_event_sequence_storage
{
    readonly protected_values _values = new();
    JsonObject _stored;
    JsonObject _released;

    void Establish() => _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation)
        .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, _values.Schema));

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
        await using (var context = CreateContext())
        {
            var entry = await context.Events.SingleAsync();
            _stored = JsonNode.Parse(entry.Content)!["1"]!.AsObject();
        }
        var read = await _storage.GetEventAt(EventSequenceNumber.First);
        _released = await _values.Release(read.Content);
    }

    [Fact] void should_store_ciphertext_for_every_scope_and_type() => _stored.All(property => property.Value is JsonValue value && value.TryGetValue<string>(out _)).ShouldBeTrue();
    [Fact] void should_restore_every_original_json_type_and_value() => JsonNode.DeepEquals(_released, _values.Plaintext).ShouldBeTrue();
}
