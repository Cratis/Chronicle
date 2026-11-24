// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_retrying_failed_partitions_and_none_can_start : given.an_observer_to_give_commands_to
{
    Exception _error;

    void Establish()
    {
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>([(Key)"one", (Key)"two"]));
        _observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(PartitionRecoveryOutcome.ObserverQuarantined);
    }

    async Task Because() => _error = await Catch.Exception(() => Give(ObserverOperatorCommand.RetryFailedPartitions));

    [Fact] void should_attempt_every_partition() => _observer.Received(2).TryStartRecoverJobForFailedPartition(Arg.Any<Key>());
    [Fact] void should_fail_the_step() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
