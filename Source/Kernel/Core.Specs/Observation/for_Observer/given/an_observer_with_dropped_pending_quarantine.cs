// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_dropped_pending_quarantine : an_observer_with_reloadable_state
{
    async Task Establish()
    {
        _stateStorage.State.InFlightPartitions.Add(_partition);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Result<JobId, StartJobError>.Failed(StartJobError.Unknown));
        _reloadableStateStorage.SuspendNextWrite = true;

        var transition = _observer.TransitionTo<CatchingUpInFlight>();
        try
        {
            await _reloadableStateStorage.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _reloadableStateStorage.ReleaseWrite.SetException(new Exception("The transition write failed"));
        }
        var exception = await Cratis.Specifications.Catch.Exception(() => transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        exception.ShouldNotBeNull();
        (await _observer.GetCurrentState()).ShouldBeOfExactType<CatchingUpInFlight>();
        (await _observer.IsObserverQuarantined()).ShouldBeTrue();

        // The underlying job-start failure has been fixed before the authorized recovery action.
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Result<JobId, StartJobError>.Success(JobId.New()));
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }
}
