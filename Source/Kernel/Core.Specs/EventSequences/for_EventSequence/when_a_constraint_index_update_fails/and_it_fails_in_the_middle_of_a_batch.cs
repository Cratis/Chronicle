// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_update_fails;

public class and_it_fails_in_the_middle_of_a_batch : given.an_event_sequence_with_a_failing_index
{
    AppendManyResult _batchResult;

    void Establish()
    {
        var clock = new given.RepairClock();
        _eventSequence.ConstraintIndexTimeProvider = clock;

        // Only retry backoffs advance time; scheduler load cannot exhaust the turn budget.
        clock.TimerCreated = due =>
        {
            if (due.TotalMilliseconds is 100 or 400) clock.Advance(due);
        };
        _update = number => number == (EventSequenceNumber)1 ? Task.FromException(new given.SimulatedStateWriteError()) : Task.CompletedTask;
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
            Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(call.Arg<IEnumerable<EventToAppendToStorage>>().Select(e => new AppendedEvent(EventContext.From(EventStore, EventStoreNamespace, e.EventType, e.EventSourceType, e.EventSourceId, e.EventStreamType, e.EventStreamId, e.SequenceNumber, e.CorrelationId), e.Content)).ToArray())));
    }
    async Task Because()
    {
        var events = Enumerable.Range(0, 3).Select(_ => ValidatedEvent()).Select(e =>
            (e.Event, e.CompliantEvent, e.CompliantContent, _currentValidation.Establish(e.Event.EventSourceId, e.Event.EventType.Id, e.CompliantEvent))).ToList();
        _batchResult = await _eventSequence.AppendManyToStorage(events, CorrelationId.New(), [], []);
    }

    [Fact] void should_succeed() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_defer_later_updates_after_the_failed_event() => _attempts.Select(number => number.Value).ShouldEqual(new ulong[] { 0, 1, 1, 1 });
    [Fact] void should_advance_only_through_the_contiguous_success() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1);
}
