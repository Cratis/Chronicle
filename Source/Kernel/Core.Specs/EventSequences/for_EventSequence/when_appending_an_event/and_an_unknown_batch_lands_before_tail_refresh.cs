// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_an_unknown_batch_lands_before_tail_refresh : given.an_append_with_settlement
{
    EventToAppendToStorage[] _lateBatch;

    async Task Establish()
    {
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
        {
            _lateBatch = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
            return Task.FromException<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>>(new given.SimulatedStorageError());
        });
        await AppendBatch(governed: true);
        _batchResult.Errors.ShouldContain(AppendError.AppendOutcomeUnknown);
        _log.Events.ShouldBeEmpty();
        _appendedEventsQueues.ClearReceivedCalls();
        await _log.AppendMany(_lateBatch);
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => _log.GetRange(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), cancellationToken: call.ArgAt<CancellationToken>(5)));
        _failReads = true;
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_adopt_the_late_batch_tail() => _stateStorage.State.SequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] async Task should_update_the_first_events_type_tail() => (await _eventSequence.GetTailSequenceNumberForEventTypes([_eventType])).ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_update_the_second_events_type_tail() => (await _eventSequence.GetTailSequenceNumberForEventTypes([new("OtherEvent", EventTypeGeneration.First)])).ShouldEqual((EventSequenceNumber)1UL);
    [Fact] async Task should_spill_again_after_the_events_have_landed() => await _appendedEventsQueues.Received(1).SpillToCatchup();
    [Fact] void should_keep_the_next_governed_append_refused() => _result.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
}
