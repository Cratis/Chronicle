// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands;

/// <summary>
/// The other direction of the same pairing - asking to recover a partition must not quietly replay it instead. The
/// command must also hand back whatever outcome the grain reports, rather than swallowing it - an operator retrying
/// a partition through this command deserves the same honest answer the grain itself computed.
/// </summary>
public class when_recovering_a_partition : given.an_observer_grain
{
    PartitionRecoveryOutcome _result;

    void Establish() => _observer.TryStartRecoverJobForFailedPartition(Partition).Returns(PartitionRecoveryOutcome.PartitionQuarantined);

    async Task Because() => _result = await new RetryPartition(EventStore, Namespace, ObserverIdentifier, string.Empty, Partition).Handle(_grainFactory);

    [Fact] void should_recover_the_partition() => _observer.Received(1).TryStartRecoverJobForFailedPartition(Partition);
    [Fact] void should_not_replay_the_partition() => _observer.DidNotReceive().ReplayPartition(Arg.Any<Concepts.Keys.Key>());
    [Fact] void should_return_the_outcome_the_grain_reported() => _result.ShouldEqual(PartitionRecoveryOutcome.PartitionQuarantined);
}
