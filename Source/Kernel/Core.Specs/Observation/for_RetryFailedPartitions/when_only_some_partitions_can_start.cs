// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

public class when_only_some_partitions_can_start : Specification
{
    readonly ObserverKey _key = new("observer", "store", "namespace", "event-log");
    readonly Key[] _partitions = [(Key)"quarantined", (Key)"can-start", (Key)"cannot-start", (Key)"also-can-start"];
    IGrainFactory _grainFactory;
    IStorage _storage;
    IObserver _observer;
    RetryFailedPartitionsResult _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>(_partitions));
        _observer.TryStartRecoverJobForFailedPartition(_partitions[0]).Returns(PartitionRecoveryOutcome.PartitionQuarantined);
        _observer.TryStartRecoverJobForFailedPartition(_partitions[1]).Returns(PartitionRecoveryOutcome.Started);
        _observer.TryStartRecoverJobForFailedPartition(_partitions[2]).Returns(Task.FromException<PartitionRecoveryOutcome>(
            new FailedPartitionRecoveryCouldNotStart(_partitions[2], _key)));
        _observer.TryStartRecoverJobForFailedPartition(_partitions[3]).Returns(PartitionRecoveryOutcome.Started);
    }

    async Task Because() => _result = await new RetryFailedPartitions("store", "namespace", "observer", "event-log").Handle(_grainFactory, _storage);

    [Fact] void should_attempt_every_partition() => _observer.Received(4).TryStartRecoverJobForFailedPartition(Arg.Any<Key>());
    [Fact] void should_continue_after_the_quarantined_partition() => _observer.Received(1).TryStartRecoverJobForFailedPartition(_partitions[1]);
    [Fact] void should_continue_after_the_failed_job_start() => _observer.Received(1).TryStartRecoverJobForFailedPartition(_partitions[3]);
    [Fact] void should_preserve_the_failed_partition_count() => _result.FailedPartitions.ShouldEqual(4);
}
