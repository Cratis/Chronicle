// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_the_retry_timer_is_disposed : given.an_observer_with_partition_replay_jobs
{
    int _activeTimersBeforeDisposal;
    Exception _retryFailure = null!;

    void Establish()
    {
        _jobStorage.GetJobs<IReplayObserverPartition, JobState>()
            .Returns(Task.FromException<Catch<IImmutableList<JobState>, JobError>>(new InvalidOperationException("storage unavailable")));
        _stateStorage.State = _stateStorage.State with { PartitionReplayCompletions = [new(_partition, _firstJob)] };
    }

    async Task Because()
    {
        await Crash();
        var retryTimerIndex = RetryTimerIndex;
        _activeTimersBeforeDisposal = _silo.TimerRegistry.NumberOfActiveTimers;
        await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Spec"), default);
        _observer.Dispose();
        _jobStorage.ClearReceivedCalls();
        try
        {
            await _silo.TimerRegistry.FireAsync(retryTimerIndex);
        }
        catch (ObjectDisposedException exception)
        {
            _retryFailure = exception;
        }
    }

    [Fact] void should_cancel_the_retry_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_activeTimersBeforeDisposal - 1);
    [Fact] void should_not_retry_after_disposal() => _jobStorage.DidNotReceive().GetJobs<IReplayObserverPartition, JobState>();
    [Fact] void should_reject_invoking_the_cancelled_timer() => _retryFailure.ShouldNotBeNull();
    [Fact] void should_leave_the_pending_completion_durable() => _stateStorage.State.PartitionReplayCompletions!.Single().JobId.ShouldEqual(_firstJob);
}
