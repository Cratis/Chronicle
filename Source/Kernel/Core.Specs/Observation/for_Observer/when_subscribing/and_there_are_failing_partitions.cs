// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_there_are_failing_partitions : given.an_observer
{
    static Exception _error;
    static ObserverType _type;
    static FailedPartitions _failedPartitions;
    static FailedPartition _firstFailedPartition;
    static FailedPartition _secondFailedPartition;

    void Establish()
    {
        _failedPartitions = new();
        _firstFailedPartition = _failedPartitions.AddFailedPartition("some-event-source");
        _secondFailedPartition = _failedPartitions.AddFailedPartition("some-event-source2");
        _type = ObserverType.Reactor;
        _failedPartitionsStorage.State = _failedPartitions;
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
    [Fact]
    void should_start_retry_failed_partition_jobs_for_first_partition() => _jobsManager
        .Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _firstFailedPartition.Partition &&
                                                                                                                                 _.FromSequenceNumber == _firstFailedPartition.LastAttempt.SequenceNumber));
    [Fact]
    void should_start_retry_failed_partition_jobs_for_second_partition() => _jobsManager
        .Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _secondFailedPartition.Partition &&
                                                                                                                                       _.FromSequenceNumber == _secondFailedPartition.LastAttempt.SequenceNumber));
}
