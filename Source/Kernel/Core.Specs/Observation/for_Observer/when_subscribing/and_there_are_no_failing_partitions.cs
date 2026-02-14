// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_there_are_no_failing_partitions : given.an_observer
{
    static Exception _error;
    static ObserverType _type;

    void Establish()
    {
        _type = ObserverType.Reactor;
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.Subscribe<NullObserverSubscriber>(_type, [EventType.Unknown], SiloAddress.Zero));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_write_state_at_least_once() => _storageStats.Writes.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_store_type_to_state() => _definitionStorage.State.Type.ShouldEqual(_type);
    [Fact]
    async Task should_get_the_subscription()
    {
        var subscription = await _observer.GetSubscription();
        subscription.EventTypes.ShouldNotBeEmpty();
        subscription.SiloAddress.ShouldEqual(SiloAddress.Zero);
        subscription.SubscriberType.ShouldEqual(typeof(NullObserverSubscriber));
    }

    [Fact] void should_be_in_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_not_start_retry_failed_partition_jobs() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
