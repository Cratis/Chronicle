// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_commit_acknowledgment_is_lost : given.an_event_sequence_with_effect_receipts
{
    void Establish() => _commitThenFail = true;

    async Task Because() => _result = await AppendEffect();

    [Fact] void should_settle_the_unknown_outcome_from_the_receipt() => _result.EffectAlreadyRecorded.ShouldBeTrue();
    [Fact] void should_not_report_an_opaque_storage_error() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_return_the_committed_response() => _result.EffectReceipt!.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_keep_exactly_one_set_of_events() => _durableStorage.Events.Count.ShouldEqual(1);
    [Fact] void should_advance_the_next_slot_from_the_receipt() => _stateStorage.State.SequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_repair_the_event_type_tail() => _stateStorage.State.TailSequenceNumberPerEventType[_eventType.Id].Value.ShouldEqual(0UL);
    [Fact] void should_repair_the_constraint_index() => _constraintIndexSequenceNumbers.ShouldContainOnly(EventSequenceNumber.First);
    [Fact] async Task should_dispatch_the_committed_event() => await _appendedEventsQueues.Received(1).Enqueue(Arg.Is<IEnumerable<AppendedEvent>>(events => events.Count() == 1 && events.First().Context.EventType == _eventType));
    [Fact] void should_use_the_winners_source_for_constraints() => _constraintValidation.Received(1).Establish(_eventSourceId, _eventType.Id, Arg.Any<System.Dynamic.ExpandoObject>(), EventSourceType.Default, EventStreamType.All, EventStreamId.Default, null, Arg.Any<Subject?>(), Arg.Any<IReadOnlyCollection<string>?>());
}
