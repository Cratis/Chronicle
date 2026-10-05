// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_retrying_quarantine_clearance;

public class and_recovery_failed_to_query_jobs : given.an_observer_with_failed_clearance_recovery
{
    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_propagate_the_first_recovery_failure() => _clearanceError.ShouldEqual(_recoveryFailure);
    [Fact] void should_be_disconnected_after_the_failure() => _wasDisconnectedAfterFailure.ShouldBeTrue();
    [Fact] void should_retain_the_subscription_after_the_failure() => _wasSubscribedAfterFailure.ShouldBeTrue();
    [Fact] void should_not_be_quarantined_after_the_failure() => _wasQuarantinedAfterFailure.ShouldBeFalse();
    [Fact] async Task should_be_observing_after_retry() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_resume_the_stopped_catchup_job_once() => _jobsManager.Received(1).Resume(_catchupJobId);
    [Fact] void should_retry_the_failed_partition_once() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _retryablePartition));
    [Fact] void should_subscribe_to_the_queue_once() => _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
