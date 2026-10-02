// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_entering_observing;

public class and_quarantine_is_requested_during_the_missed_events_query : given.an_observer_automatically_reconciled_during_a_probe
{
    readonly TaskCompletionSource _queryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<EventSequenceNumber, GetSequenceNumberError>> _queryResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _queryCount;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 43UL };
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)42UL, (EventSequenceNumber)44UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
        {
            _queryCount++;
            if (_queryCount == 1)
            {
                return Task.FromResult(Result<EventSequenceNumber, GetSequenceNumberError>.Success(EventSequenceNumber.Unavailable));
            }

            _queryEntered.TrySetResult();
            return _queryResult.Task;
        });
    }

    async Task Because()
    {
        var transition = _observer.TransitionTo<Routing>();
        try
        {
            await _queryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.FailedPartitionRecovered(_partition, 42UL);
        }
        finally
        {
            _queryResult.SetResult((EventSequenceNumber)43UL);
        }
        await transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_enter_the_requested_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_persist_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_recovery_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_route_back_to_the_queue_after_the_query() => _appendedEventsQueues.Received(1)
        .Subscribe(_observerKey, Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
}
