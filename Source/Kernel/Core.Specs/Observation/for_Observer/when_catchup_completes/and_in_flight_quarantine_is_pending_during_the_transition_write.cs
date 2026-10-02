// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catchup_completes;

public class and_in_flight_quarantine_is_pending_during_the_transition_write : given.an_observer_with_reloadable_state
{
    bool _wasQuarantinedWhileTransitionWasPending;

    void Establish()
    {
        _stateStorage.State.InFlightPartitions.Add(_partition);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Result<JobId, StartJobError>.Failed(StartJobError.Unknown));
        _reloadableStateStorage.SuspendNextWrite = true;
    }

    async Task Because()
    {
        var transition = _observer.TransitionTo<CatchingUpInFlight>();
        try
        {
            await _reloadableStateStorage.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _wasQuarantinedWhileTransitionWasPending = await _observer.IsObserverQuarantined();
            await _observer.CaughtUp(42UL);
        }
        finally
        {
            _reloadableStateStorage.ReleaseWrite.SetResult();
        }
        await transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_treat_the_pending_request_as_quarantine() => _wasQuarantinedWhileTransitionWasPending.ShouldBeTrue();
    [Fact] async Task should_enter_the_scheduled_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_record_completion_progress() => _reloadableStateStorage.PersistedState.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_route_back_to_the_queue() => ShouldNotSubscribeToQueue();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_replacement_catchup() => ShouldNotStartCatchup();
}
