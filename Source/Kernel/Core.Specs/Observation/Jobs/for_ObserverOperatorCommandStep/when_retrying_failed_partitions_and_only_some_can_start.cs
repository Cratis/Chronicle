// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_retrying_failed_partitions_and_only_some_can_start : given.an_observer_to_give_commands_to
{
    static readonly Key[] _partitions = [(Key)"one", (Key)"two", (Key)"three", (Key)"four"];

    void Establish()
    {
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>(_partitions));
        _observer.TryStartRecoverJobForFailedPartition(_partitions[0]).Returns(PartitionRecoveryOutcome.PartitionQuarantined);
        _observer.TryStartRecoverJobForFailedPartition(_partitions[1]).Returns(PartitionRecoveryOutcome.Started);
        _observer.TryStartRecoverJobForFailedPartition(_partitions[2]).Returns(Task.FromException<PartitionRecoveryOutcome>(
            new FailedPartitionRecoveryCouldNotStart(_partitions[2], ObserverKey.NotSet)));
        _observer.TryStartRecoverJobForFailedPartition(_partitions[3]).Returns(PartitionRecoveryOutcome.Started);
    }

    async Task Because() => await Give(ObserverOperatorCommand.RetryFailedPartitions);

    [Fact] void should_attempt_every_partition() => _observer.Received(4).TryStartRecoverJobForFailedPartition(Arg.Any<Key>());
    [Fact] void should_continue_after_the_quarantined_partition() => _observer.Received(1).TryStartRecoverJobForFailedPartition(_partitions[1]);
    [Fact] void should_continue_after_the_failed_job_start() => _observer.Received(1).TryStartRecoverJobForFailedPartition(_partitions[3]);
}
