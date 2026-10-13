// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_non_string_security_values_are_encrypted : given.an_event_sequence_with_security_values
{
    AppendResult _result;
    JsonObject _stored;
    ExpandoObject _released;

    async Task Because()
    {
        _result = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, _plaintext, CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
        if (!_result.IsSuccess)
        {
            return;
        }
        var read = await _memory.GetEventAt(_result.SequenceNumber);
        _stored = _converter.ToJsonObject(read.Content, _schema);
        var released = await _manager.Release(EventStore, EventStoreNamespace, _schema, read.Context.Subject.Value, _stored);
        _released = _converter.ToExpandoObject(released, _schema);
    }

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_only_ciphertext() => _stored.All(property => property.Value is JsonValue value && value.TryGetValue<string>(out _)).ShouldBeTrue();
    [Fact] void should_release_the_subject_integer() => ((IDictionary<string, object?>)_released)["subjectCount"].ShouldEqual(37);
    [Fact] void should_release_the_subject_boolean() => ((IDictionary<string, object?>)_released)["subjectFlag"].ShouldEqual(true);
    [Fact] void should_release_the_namespace_integer() => ((IDictionary<string, object?>)_released)["namespaceCount"].ShouldEqual(38);
    [Fact] void should_release_the_namespace_boolean() => ((IDictionary<string, object?>)_released)["namespaceFlag"].ShouldEqual(false);
    [Fact] void should_release_the_global_integer() => ((IDictionary<string, object?>)_released)["globalCount"].ShouldEqual(39);
    [Fact] void should_release_the_global_boolean() => ((IDictionary<string, object?>)_released)["globalFlag"].ShouldEqual(true);
}
