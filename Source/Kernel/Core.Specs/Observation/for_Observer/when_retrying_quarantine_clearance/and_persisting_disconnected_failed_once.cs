// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_retrying_quarantine_clearance;

public class and_persisting_disconnected_failed_once : given.an_observer_whose_quarantine_leave_fails_to_persist
{
    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_propagate_the_write_failure() => _clearanceError.ShouldEqual(_writeFailure);
    [Fact] void should_be_disconnected_after_the_failure() => _wasDisconnectedAfterFailure.ShouldBeTrue();
    [Fact] void should_not_be_quarantined_after_the_failure() => _wasQuarantinedAfterFailure.ShouldBeFalse();
    [Fact] void should_retain_the_subscription_after_the_failure() => _wasSubscribedAfterFailure.ShouldBeTrue();
    [Fact] void should_still_owe_recovery_after_the_failure() => _owedRecoveryAfterFailure.ShouldBeTrue();
    [Fact] async Task should_be_observing_after_retry() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_no_longer_owe_recovery_after_retry() => _observer.OwesRecoveryAfterQuarantine().ShouldBeFalse();
    [Fact] void should_persist_active_state() => _faultableStateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_retry_the_failed_partition() => _jobsManager.Received().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _retryablePartition));
    [Fact] void should_subscribe_to_the_queue_once() => _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
