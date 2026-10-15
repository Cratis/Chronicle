// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_observer_quarantine_during_entry;

public class and_wiped_storage_routing_supersedes_the_leave : given.an_observer_entering_quarantine
{
    readonly given.reloadable_observer_state_storage _reloadableStateStorage = new();
    bool _wasObservingBeforeUnsubscribe;

    public and_wiped_storage_routing_supersedes_the_leave()
    {
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _reloadableStateStorage : null!;
    }

    async Task Because()
    {
        try
        {
            await _observer.ClearObserverQuarantine().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _reloadableStateStorage.ClearStateAsync();
            await _observer.Subscribe<NullObserverSubscriber>(ObserverType.External, [EventType.Unknown], SiloAddress.Zero, automatic: true);
        }
        finally
        {
            _cleanupJobs.SetResult(_jobs);
        }
        await _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _wasObservingBeforeUnsubscribe = await _observer.GetCurrentState() is Observing;
        await _observer.Unsubscribe();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_route_after_the_storage_reset() => _wasObservingBeforeUnsubscribe.ShouldBeTrue();
    [Fact] async Task should_end_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] async Task should_remain_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_resume_jobs_on_unsubscribe() => _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] void should_not_retry_failed_partitions_on_unsubscribe() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_not_start_in_flight_catchup_on_unsubscribe() => CheckDidNotStartCatchupJob();
    [Fact] async Task should_not_renew_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
}
