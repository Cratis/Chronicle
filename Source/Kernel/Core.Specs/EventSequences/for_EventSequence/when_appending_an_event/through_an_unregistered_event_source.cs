// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class through_an_unregistered_event_source : given.an_event_sequence
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
        ConcurrencyScope.None,
        null,
        null,
        [],
        eventSource: new EventSourceName("Nope"));

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_an_unknown_event_source() => _result.Errors.Single().ToString().ShouldContain("UnknownEventSource");
    [Fact] void should_not_persist_the_event() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
