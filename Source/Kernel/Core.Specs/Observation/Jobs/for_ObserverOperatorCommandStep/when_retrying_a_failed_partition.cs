// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_retrying_a_failed_partition : given.an_observer_to_give_commands_to
{
    void Establish() => _observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(PartitionRecoveryOutcome.Started);

    async Task Because() => await Give(ObserverOperatorCommand.RetryPartition, "failed-partition");

    [Fact] void should_retry_that_partition() => _observer.Received(1).TryStartRecoverJobForFailedPartition("failed-partition");
}
