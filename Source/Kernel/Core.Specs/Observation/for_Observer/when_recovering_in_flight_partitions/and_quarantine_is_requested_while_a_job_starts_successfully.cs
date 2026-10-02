// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_in_flight_partitions;

public class and_quarantine_is_requested_while_a_job_starts_successfully : given.an_observer_with_reloadable_state
{
    readonly TaskCompletionSource _startEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<JobId, StartJobError>> _startResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _wasQuarantinedWhileStarting;

    void Establish()
    {
        _stateStorage.State.InFlightPartitions.Add(_partition);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()).Returns(_ =>
        {
            _startEntered.SetResult();
            return _startResult.Task;
        });
    }

    async Task Because()
    {
        var transition = _observer.TransitionTo<CatchingUpInFlight>();
        try
        {
            await _startEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.FailedPartitionRecovered(_partition, 42UL);
            _wasQuarantinedWhileStarting = await _observer.IsObserverQuarantined();
        }
        finally
        {
            _startResult.SetResult(Result<JobId, StartJobError>.Success(JobId.New()));
        }
        await transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_treat_the_pending_request_as_quarantine() => _wasQuarantinedWhileStarting.ShouldBeTrue();
    [Fact] async Task should_enter_the_requested_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_persist_quarantine() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_recovery_progress() => _reloadableStateStorage.PersistedState.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_route_back_to_the_queue() => ShouldNotSubscribeToQueue();
    [Fact] void should_not_start_replacement_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
}
