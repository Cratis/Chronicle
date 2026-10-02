// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_observer_quarantine_during_entry;

public class and_clearance_is_repeated_while_unsubscribe_pauses_jobs : given.an_observer_entering_quarantine
{
    readonly TaskCompletionSource _pauseEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IImmutableList<JobState>> _pauseJobs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _stateStorage.State.InFlightPartitions.Add((Key)"in-flight-partition");
        _jobsManager.GetAllJobs().Returns(_ => _pauseEntered.TrySetResult() ? _pauseJobs.Task : Task.FromResult(_jobs));
    }

    async Task Because()
    {
        try
        {
            await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            var unsubscribe = _observer.Unsubscribe();
            try
            {
                await _pauseEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

                // Clearance reinstates the pending recovery after Unsubscribe canceled it, but before the subscription is removed.
                await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            }
            finally
            {
                _pauseJobs.SetResult(_jobs);
            }
            await unsubscribe.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _cleanupJobs.SetResult(_jobs);
        }
        await _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_end_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] async Task should_remain_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_resume_jobs() => _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] void should_not_retry_failed_partitions() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_not_start_in_flight_catchup() => CheckDidNotStartCatchupJob();
    [Fact] async Task should_not_renew_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
}
