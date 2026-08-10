// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_plaintext_does_not_match_the_schema : given.an_event_sequence_with_a_compliant_enum
{
    AppendResult _result;

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_reject_the_event() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_apply_compliance_to_invalid_plaintext() =>
        _complianceManager.DidNotReceive().Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Cratis.Chronicle.Schemas.JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
}
