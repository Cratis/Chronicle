// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

/// <summary>
/// The warm-start state snapshot is taken after the event is durable and after the sequence number has advanced, and
/// it is only an optimization - the sequence number is re-derived from the event tail on the next activation. A
/// failure there must therefore neither fail the append, which would make a retrying client append the event twice,
/// nor stop the live dispatch and the constraint-index update that follow it.
/// </summary>
public class and_the_state_snapshot_fails : given.an_event_sequence_that_cannot_persist_state
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

    [Fact] void should_report_the_durable_append_as_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_sequence_number_the_event_got() => _result.SequenceNumber.ShouldEqual(_appendedSequenceNumber);
    [Fact] void should_still_dispatch_the_event_for_live_delivery() => _appendedEventsQueues.Received(1).Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_still_update_the_constraint_index_once() => _constraintIndexSequenceNumbers.Count.ShouldEqual(1);
    [Fact] void should_still_index_with_the_appended_sequence_number() => _constraintIndexSequenceNumbers[0].ShouldEqual(_appendedSequenceNumber);
}
