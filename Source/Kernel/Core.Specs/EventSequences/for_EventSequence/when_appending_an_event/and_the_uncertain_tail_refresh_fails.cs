// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_the_uncertain_tail_refresh_fails : given.an_append_with_settlement
{
    AppendResult _firstResult;
    EventSequenceNumber _nextBeforeRefresh;
    EventSequenceNumber _nextAfterFailure;
    int _tailRefreshAttempts;

    async Task Establish()
    {
        _empty = true;
        await AppendBatch();
        _batchResult.Errors.ShouldContain(AppendError.AppendOutcomeUnknown);
        await Seed(EventSequenceNumber.First);
        _eventType = new("UngovernedEvent", EventTypeGeneration.First);
        _nextBeforeRefresh = _stateStorage.State.SequenceNumber;
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _tailRefreshAttempts++;
                return Task.FromException<IEventCursor>(new given.SimulatedStorageError());
            });
    }

    async Task Because()
    {
        _firstResult = await AppendAnEvent();
        _nextAfterFailure = _stateStorage.State.SequenceNumber;
        _result = await AppendAnEvent();
    }

    [Fact] void should_fail_the_first_append() => _firstResult.HasErrors.ShouldBeTrue();
    [Fact] void should_leave_the_sequence_number_unchanged_after_failure() => _nextAfterFailure.ShouldEqual(_nextBeforeRefresh);
    [Fact] void should_retry_the_refresh_on_the_next_append() => _tailRefreshAttempts.ShouldEqual(2);
    [Fact] void should_fail_the_next_append_while_the_refresh_keeps_failing() => _result.HasErrors.ShouldBeTrue();
    [Fact] void should_leave_the_sequence_number_unchanged_after_the_retry() => _stateStorage.State.SequenceNumber.ShouldEqual(_nextBeforeRefresh);
    [Fact] void should_not_write_either_refused_append() => _log.Events.Count.ShouldEqual(1);
}
