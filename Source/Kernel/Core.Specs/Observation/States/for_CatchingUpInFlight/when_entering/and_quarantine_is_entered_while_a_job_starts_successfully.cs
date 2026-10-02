// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_quarantine_is_entered_while_a_job_starts_successfully : given.a_catching_up_in_flight_state
{
    readonly TaskCompletionSource _startEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<JobId, StartJobError>> _startResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverState _quarantinedState;

    void Establish()
    {
        _storedState.InFlightPartitions.Add((Key)"in-flight-partition");
        _quarantinedState = _storedState with { RunningState = ObserverRunningState.Quarantined };
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()).Returns(_ =>
        {
            _startEntered.SetResult();
            return _startResult.Task;
        });
    }

    async Task Because()
    {
        var entry = _state.OnEnter(_storedState);
        try
        {
            await _startEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

            // Represent an activation that has entered quarantine, not a merely scheduled request.
            _observer.IsObserverQuarantined().Returns(true);
            _observer.GetState().Returns(_quarantinedState);
        }
        finally
        {
            _startResult.SetResult(Result<JobId, StartJobError>.Success(JobId.New()));
        }
        _resultingStoredState = await entry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_keep_the_entered_quarantine_state() => _resultingStoredState.ShouldEqual(_quarantinedState);
    [Fact] void should_not_route_over_quarantine() => _stateMachine.DidNotReceive().TransitionTo<Routing>();
    [Fact] void should_not_request_another_quarantine() => _stateMachine.DidNotReceive().TransitionTo<QuarantinedObserver>();
}
