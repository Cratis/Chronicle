// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Subscribing a quarantined observer after its grain was reactivated must do what subscribing any other observer
/// does, including recovering the partitions that failed before it was quarantined.
/// </summary>
public class and_the_quarantined_observer_was_reactivated_and_has_failed_partitions : given.a_reactivated_quarantined_observer
{
    FailedPartition _failedPartition;
    FailedPartition _quarantinedPartition;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        _failedPartition = failedPartitions.AddFailedPartition("some-event-source");
        _quarantinedPartition = failedPartitions.AddFailedPartition("some-quarantined-event-source");
        failedPartitions.Quarantine(_quarantinedPartition.Partition);
        _failedPartitionsStorage.State = failedPartitions;
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_be_in_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_start_recovering_the_failed_partition() => _jobsManager
        .Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _failedPartition.Partition));

    [Fact]
    void should_not_recover_the_partition_that_is_quarantined_itself() => _jobsManager
        .DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _quarantinedPartition.Partition));
}
