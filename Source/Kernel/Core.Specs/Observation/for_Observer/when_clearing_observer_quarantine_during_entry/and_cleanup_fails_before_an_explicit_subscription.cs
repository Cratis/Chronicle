// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_observer_quarantine_during_entry;

public class and_cleanup_fails_before_an_explicit_subscription : given.an_observer_entering_quarantine
{
    readonly Exception _cleanupFailure = new InvalidOperationException("Quarantine cleanup failed");
    Exception? _cleanupError;
    bool _wasQuarantinedAfterCleanup;

    async Task Because()
    {
        try
        {
            await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _cleanupJobs.SetException(_cleanupFailure);
        }
        _cleanupError = await Catch.Exception(() => _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        _wasQuarantinedAfterCleanup = await _observer.IsObserverQuarantined();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.External, [EventType.Unknown], SiloAddress.Zero, "new-subscription");
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_propagate_the_cleanup_failure() => _cleanupError.ShouldEqual(_cleanupFailure);
    [Fact] void should_remain_quarantined_after_failed_cleanup() => _wasQuarantinedAfterCleanup.ShouldBeTrue();
    [Fact] async Task should_be_observing_after_subscription() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_use_the_new_subscription() => (await _observer.GetSubscription()).Arguments.ShouldEqual("new-subscription");
    [Fact] void should_subscribe_to_the_queue_once() => _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_resume_the_stopped_catchup_job_once() => _jobsManager.Received(1).Resume(_catchupJobId);
    [Fact] void should_resume_the_stopped_retry_job_once() => _jobsManager.Received(1).Resume(_retryJobId);
    [Fact] void should_retry_the_failed_partition_once() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _retryablePartition));
}
