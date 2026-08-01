// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Api.Observation.for_ObserverCommands;

/// <summary>
/// Recovering a failed partition has to retry it, not replay it. A replay re-delivers events the observer has
/// already handled and, for a reactor registered as non-replayable, short-circuits and does nothing at all —
/// so asking to recover would silently leave the partition failed.
/// </summary>
public class when_trying_to_recover_a_failed_partition : given.observer_commands
{
    async Task Because() => await _commands.TryRecoverFailedPartition(EventStore, Namespace, ObserverId, Partition);

    [Fact] void should_retry_the_partition() => _observers.Received(1).RetryPartition(
        Arg.Is<RetryPartition>(request =>
            request.EventStore == EventStore &&
            request.Namespace == Namespace &&
            request.ObserverId == ObserverId &&
            request.Partition == Partition));

    [Fact] void should_not_replay_the_partition() => _observers.DidNotReceive().ReplayPartition(Arg.Any<ReplayPartition>());
}
