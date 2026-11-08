// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_import_chunk;

public class and_part_of_it_was_committed_before_an_interruption : given.an_event_sequence_with_imports
{
    async Task Establish()
    {
        await _log.Append(
            0,
            EventSourceType.Default,
            _events[0].EventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [ImportCausation(0)],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [_eventType.Generation] = new() },
            new Dictionary<EventTypeGeneration, EventHash>());
        _stateStorage.State.SequenceNumber = 1;
    }

    async Task Because() => _chunkResult = await AppendChunk(committedFrom: EventSequenceNumber.First, mayBeCommitted: true);

    [Fact] void should_settle() => _chunkResult.IsSettled.ShouldBeTrue();
    [Fact] void should_report_the_stored_event_as_appended_at_its_place() => _chunkResult.Outcomes.Single(_ => _.Index == 0).SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_append_it_again() => _written.Select(_ => _.EventSourceId).ShouldContainOnly(_events[1].EventSourceId, _events[2].EventSourceId);
    [Fact] void should_report_every_event_as_appended() => _chunkResult.Outcomes.All(_ => _.Kind == ImportEventOutcomeKind.Appended).ShouldBeTrue();
    [Fact] async Task should_hand_the_observers_of_the_stored_event_to_catch_up_in_case_the_interrupted_attempt_did_not() => await _appendedEventsQueues.Received().SpillMatchingToCatchup(Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Any(e => e.Context.SequenceNumber == EventSequenceNumber.First)));
}
