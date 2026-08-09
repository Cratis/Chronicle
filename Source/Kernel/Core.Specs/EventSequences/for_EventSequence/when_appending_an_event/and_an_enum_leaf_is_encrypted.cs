// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_an_enum_leaf_is_encrypted : given.an_event_sequence_with_a_compliant_enum
{
    AppendResult _result;

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        ValidContent(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_convert_the_ciphertext_without_coercing_it_to_the_enum() =>
        _expandoObjectConverter.Received().ToExpandoObject(
            Arg.Is<System.Text.Json.Nodes.JsonObject>(_ => HasEncryptedStatus(_)),
            _compliantEnumSchema);

    static bool HasEncryptedStatus(System.Text.Json.Nodes.JsonObject content) =>
        content["status"] is System.Text.Json.Nodes.JsonValue value &&
        value.TryGetValue<string>(out var ciphertext) &&
        ciphertext != "0";
}
