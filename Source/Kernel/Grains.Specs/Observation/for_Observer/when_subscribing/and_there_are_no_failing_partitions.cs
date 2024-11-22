// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_subscribing;

public class and_there_are_no_failing_partitions : given.an_observer
{
    static Exception error;
    static ObserverType type;

    void Establish()
    {
        type = ObserverType.Client;
    }

    async Task Because() => error = await Catch.Exception(() => observer.Subscribe<NullObserverSubscriber>(type, [], SiloAddress.Zero));

    [Fact] void should_not_fail() => error.ShouldBeNull();
    [Fact] void should_write_state_at_least_once() => storage_stats.Writes.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_store_type_to_state() => state_storage.State.Type.ShouldEqual(type);
    [Fact] async Task should_get_the_subscription()
    {
        var subscription = await observer.GetSubscription();
        subscription.EventTypes.ShouldBeEmpty();
        subscription.SiloAddress.ShouldEqual(SiloAddress.Zero);
        subscription.SubscriberType.ShouldEqual(typeof(NullObserverSubscriber));
    }

    [Fact] void should_be_in_running_state() => state_storage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_not_start_retry_failed_partition_jobs() => jobsManager.Verify(_ => _.Start<IRetryFailedPartitionJob, RetryFailedPartitionRequest>(IsAny<JobId>(), IsAny<RetryFailedPartitionRequest>()), Never);
}