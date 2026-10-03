// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// A client whose subscribe times out retries it, and the subscribe it gave up on still runs. Every one of them asked
/// for a full recovery of its own, piling more work onto an observer that was already too busy to answer - so the
/// requests made before recovery runs must amount to a single recovery.
/// </summary>
public class and_it_is_requested_repeatedly_before_it_runs : given.an_observer
{
    static FailedPartition _firstFailedPartition;
    static FailedPartition _secondFailedPartition;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        _firstFailedPartition = failedPartitions.AddFailedPartition("some-event-source");
        _secondFailedPartition = failedPartitions.AddFailedPartition("some-event-source2");
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 2 };
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.TryRecoverAllFailedPartitions();
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact]
    void should_recover_the_first_partition_once() => _jobsManager
        .Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _firstFailedPartition.Partition));
    [Fact]
    void should_recover_the_second_partition_once() => _jobsManager
        .Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _secondFailedPartition.Partition));
}
