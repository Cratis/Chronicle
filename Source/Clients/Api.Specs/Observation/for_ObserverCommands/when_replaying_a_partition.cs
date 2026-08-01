// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Api.Observation.for_ObserverCommands;

/// <summary>
/// The other direction of the same pairing — asking to replay a partition must not quietly retry it instead.
/// </summary>
public class when_replaying_a_partition : given.observer_commands
{
    async Task Because() => await _commands.ReplayPartition(EventStore, Namespace, ObserverId, Partition);

    [Fact] void should_replay_the_partition() => _observers.Received(1).ReplayPartition(
        Arg.Is<ReplayPartition>(request =>
            request.EventStore == EventStore &&
            request.Namespace == Namespace &&
            request.ObserverId == ObserverId &&
            request.Partition == Partition));

    [Fact] void should_not_retry_the_partition() => _observers.DidNotReceive().RetryPartition(Arg.Any<RetryPartition>());
}
