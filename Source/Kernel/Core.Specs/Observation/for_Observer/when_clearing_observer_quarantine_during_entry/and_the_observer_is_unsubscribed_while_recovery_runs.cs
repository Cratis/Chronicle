// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_observer_quarantine_during_entry;

public class and_the_observer_is_unsubscribed_while_recovery_runs : given.an_observer_entering_quarantine
{
    readonly TaskCompletionSource _recoveryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<EventSequenceNumber> _tailSequenceNumber = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _stateStorage.State.InFlightPartitions.Add((Key)"in-flight-partition");

        // Recovery from the Disconnected entry hook starts by evaluating replay against the tail. Holding it there
        // keeps the recovery running in the interleaved quarantine entry while the observer is unsubscribed.
        _eventSequence.GetTailSequenceNumber().Returns(_ => _recoveryEntered.TrySetResult()
            ? _tailSequenceNumber.Task
            : Task.FromResult(EventSequenceNumber.Unavailable));
    }

    async Task Because()
    {
        try
        {
            await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _cleanupJobs.SetResult(_jobs);
            await _recoveryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.Unsubscribe().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _cleanupJobs.TrySetResult(_jobs);
            _tailSequenceNumber.TrySetResult(EventSequenceNumber.Unavailable);
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
    [Fact] void should_not_owe_recovery() => _observer.OwesRecoveryAfterQuarantine().ShouldBeFalse();
    [Fact] void should_not_rejoin_the_queue() => _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
