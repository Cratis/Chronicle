// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_retrying_quarantine_clearance;

public class and_unsubscribe_superseded_recovery : given.an_observer_with_failed_clearance_recovery
{
    async Task Establish() => await _observer.Unsubscribe();

    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_remain_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] async Task should_remain_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_resume_jobs() => _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
    [Fact] void should_not_retry_failed_partitions() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_not_start_in_flight_catchup() => CheckDidNotStartCatchupJob();
    [Fact] void should_not_subscribe_to_the_queue() => _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
