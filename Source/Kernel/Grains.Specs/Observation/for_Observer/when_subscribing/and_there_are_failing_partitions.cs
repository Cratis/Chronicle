// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_subscribing;

public class and_there_are_failing_partitions : given.an_observer
{
    static Exception error;
    static ObserverType type;
    static FailedPartitions failedPartitions;
    static FailedPartition firstFailedPartition;
    static FailedPartition secondFailedPartition;

    void Establish()
    {
        failedPartitions = new();
        firstFailedPartition = failedPartitions.AddFailedPartition("some-event-source");
        secondFailedPartition = failedPartitions.AddFailedPartition("some-event-source2");
        type = ObserverType.Reactor;
        _failedPartitionsStorage.State = failedPartitions;
    }

    async Task Because() => error = await Catch.Exception(() => _observer.Subscribe<NullObserverSubscriber>(type, [], SiloAddress.Zero));

    [Fact] void should_not_fail() => error.ShouldBeNull();
    [Fact] void should_write_state_at_least_once() => _storageStats.Writes.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_store_type_to_state() => _stateStorage.State.Type.ShouldEqual(type);
    [Fact] async Task should_get_the_subscription()
    {
        var subscription = await _observer.GetSubscription();
        subscription.EventTypes.ShouldBeEmpty();
        subscription.SiloAddress.ShouldEqual(SiloAddress.Zero);
        subscription.SubscriberType.ShouldEqual(typeof(NullObserverSubscriber));
    }

    [Fact] void should_be_in_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_start_retry_failed_partition_jobs_for_first_partition() => _jobsManager
        .Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<JobId>(), Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == firstFailedPartition.Partition &&
                                                                                                                                 _.FromSequenceNumber == firstFailedPartition.LastAttempt.SequenceNumber));
    [Fact] void should_start_retry_failed_partition_jobs_for_second_partition() => _jobsManager
        .Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<JobId>(), Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == secondFailedPartition.Partition &&
                                                                                                                                       _.FromSequenceNumber == secondFailedPartition.LastAttempt.SequenceNumber));
}
