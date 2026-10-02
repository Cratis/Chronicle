// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_observer_quarantine_during_entry;

public class and_the_subscription_is_unchanged : given.an_observer_entering_quarantine
{
    bool _wasQuarantinedAfterClearance;

    async Task Because()
    {
        try
        {
            await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _wasQuarantinedAfterClearance = await _observer.IsObserverQuarantined();
        }
        finally
        {
            _cleanupJobs.SetResult(_jobs);
        }
        await _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_defer_recovery_until_quarantine_cleanup_finishes() => _wasQuarantinedAfterClearance.ShouldBeTrue();
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_the_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] async Task should_keep_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_leave_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_subscribe_to_the_queue() => _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_resume_the_stopped_catchup_job() => _jobsManager.Received(1).Resume(_catchupJobId);
    [Fact] void should_resume_the_stopped_retry_job() => _jobsManager.Received(1).Resume(_retryJobId);
    [Fact] void should_retry_the_failed_partition() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _retryablePartition));
}
