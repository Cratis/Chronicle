// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

/// <summary>
/// Handing the appended event to the queues happens after it is durable and after the sequence number has advanced.
/// A fault there must therefore not be able to report the append as failed - a retrying client would append the event
/// twice - and it must not take the constraint-index update with it, or a durable value would be invisible to unique
/// validation and a later duplicate would pass. The lost live delivery is recovered by spilling to catch-up.
/// </summary>
public class and_the_live_dispatch_fails : given.an_event_sequence
{
    AppendResult _result;

    void Establish() => _appendedEventsQueues
        .Enqueue(Arg.Any<IEnumerable<AppendedEvent>>())
        .Returns(_ => Task.FromException(new given.SimulatedEnqueueError()));

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
    [Fact] void should_still_update_the_constraint_index_once() => _constraintIndexSequenceNumbers.Count.ShouldEqual(1);
    [Fact] void should_still_index_with_the_appended_sequence_number() => _constraintIndexSequenceNumbers[0].ShouldEqual(_appendedSequenceNumber);
    [Fact] void should_spill_the_queues_to_catch_up() => _appendedEventsQueues.Received(1).SpillToCatchup();
}
