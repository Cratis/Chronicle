// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Specs.Compliance.given;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_round_tripping_non_string_security_values : Specification
{
    readonly protected_values _values = new();
    EventSequenceStorage _storage;
    AppendedEvent _read;
    JsonObject _released;

    void Establish() => _storage = new(protected_values.EventStore, protected_values.Namespace, EventSequenceId.Log, new IdentityStorage());

    async Task Because()
    {
        var encrypted = await _values.Encrypt();
        (await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            protected_values.Subject,
            EventStreamType.All,
            EventStreamId.Default,
            new EventType("protected-values", EventTypeGeneration.First),
            CorrelationId.NotSet,
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = encrypted },
            new Dictionary<EventTypeGeneration, EventHash>())).IsSuccess.ShouldBeTrue();
        _read = await _storage.GetEventAt(EventSequenceNumber.First);
        _released = await _values.Release(_read.Content);
    }

    [Fact] void should_store_ciphertext_for_every_scope_and_type() => ((IDictionary<string, object?>)_read.Content).Values.All(value => value is string).ShouldBeTrue();
    [Fact] void should_restore_every_original_json_type_and_value() => JsonNode.DeepEquals(_released, _values.Plaintext).ShouldBeTrue();
}
