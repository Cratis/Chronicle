// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands;

/// <summary>
/// The other direction of the same pairing - asking to recover a partition must not quietly replay it instead.
/// </summary>
public class when_recovering_a_partition : given.an_observer_grain
{
    async Task Because() => await new RetryPartition(EventStore, Namespace, ObserverIdentifier, string.Empty, Partition).Handle(_grainFactory);

    [Fact] void should_recover_the_partition() => _observer.Received(1).TryStartRecoverJobForFailedPartition(Partition);
    [Fact] void should_not_replay_the_partition() => _observer.DidNotReceive().ReplayPartition(Arg.Any<Concepts.Keys.Key>());
}
