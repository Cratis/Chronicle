// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.States.for_Observing.when_entering;

public class and_quarantine_is_entered_during_the_missed_events_query : given.an_observing_state
{
    readonly TaskCompletionSource _queryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<EventSequenceNumber, GetSequenceNumberError>> _queryResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverState _quarantinedState;

    void Establish()
    {
        _storedState = _storedState with { NextEventSequenceNumber = 43UL };
        _quarantinedState = _storedState with { RunningState = ObserverRunningState.Quarantined };
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)44UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
        {
            _queryEntered.SetResult();
            return _queryResult.Task;
        });
    }

    async Task Because()
    {
        var entry = _state.OnEnter(_storedState);
        try
        {
            await _queryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

            // Represent an activation that has entered quarantine, not a merely scheduled request.
            _observer.IsObserverQuarantined().Returns(true);
            _observer.GetState().Returns(_quarantinedState);
        }
        finally
        {
            _queryResult.SetResult((EventSequenceNumber)43UL);
        }
        _resultingStoredState = await entry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_keep_the_entered_quarantine_state() => _resultingStoredState.ShouldEqual(_quarantinedState);
    [Fact] void should_not_route_over_quarantine() => _stateMachine.DidNotReceive().TransitionTo<Routing>();
}
