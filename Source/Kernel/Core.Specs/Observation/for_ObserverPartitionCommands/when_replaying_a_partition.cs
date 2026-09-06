// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands;

/// <summary>
/// Replaying a partition and recovering one are separate operations on the observer - asking for the one must not
/// quietly do the other.
/// </summary>
public class when_replaying_a_partition : given.an_observer_grain
{
    async Task Because() => await new ReplayPartition(EventStore, Namespace, ObserverIdentifier, string.Empty, Partition).Handle(_grainFactory);

    [Fact] void should_replay_the_partition() => _observer.Received(1).ReplayPartition(Partition);
    [Fact] void should_not_recover_the_partition() => _observer.DidNotReceive().TryStartRecoverJobForFailedPartition(Arg.Any<Concepts.Keys.Key>());
}
