// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_retrying_a_partition_that_is_not_failed : given.an_observer_to_give_commands_to
{
    Exception _error;

    void Establish() => _observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(PartitionRecoveryOutcome.PartitionNotFound);

    async Task Because() => _error = await Catch.Exception(() => Give(ObserverOperatorCommand.RetryPartition, "healthy-partition"));

    [Fact] void should_fail_the_step() => _error.ShouldBeOfExactType<FailedPartitionRecoveryCouldNotStart>();
    [Fact] void should_identify_the_partition() => _error.Message.ShouldContain("healthy-partition");
}
