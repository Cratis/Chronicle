// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Recovering a failed partition starts a job through the jobs manager, one per partition. An
/// observer carrying hundreds of them spent longer than a grain call's 30 second budget in that
/// loop, so Subscribe never returned to the client that called it - which timed out, retried, and
/// left the observer recorded as never subscribed. The subscription must be established, and the
/// call must return, before any recovery is attempted.
/// </summary>
public class and_there_are_failing_partitions_whose_recovery_has_not_run_yet : given.an_observer
{
    static Exception _error;
    static FailedPartitions _failedPartitions;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartitions.AddFailedPartition("some-event-source");
        _failedPartitions.AddFailedPartition("some-event-source2");
        _failedPartitionsStorage.State = _failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 2 };
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_be_in_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact]
    async Task should_have_established_the_subscription()
    {
        var subscription = await _observer.GetSubscription();
        subscription.SubscriberType.ShouldEqual(typeof(NullObserverSubscriber));
    }

    [Fact]
    void should_not_have_started_recovering_any_partition_within_the_subscribe_call() => _jobsManager
        .DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());

    [Fact] void should_have_scheduled_the_recovery_for_a_turn_of_its_own() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldBeGreaterThanOrEqual(1);
}
