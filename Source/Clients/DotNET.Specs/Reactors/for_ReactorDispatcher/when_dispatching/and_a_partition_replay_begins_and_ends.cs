// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

public class and_a_partition_replay_begins_and_ends : given.a_reactor_dispatcher
{
    async Task Because()
    {
        await _dispatcher.Dispatch(_handler, null, new() { ReplayState = ReplayState.BeginReplayPartition, Partition = "x" }, "ns", Publish, CancellationToken.None);
        await _dispatcher.Dispatch(_handler, null, new() { ReplayState = ReplayState.EndReplayPartition, Partition = "x" }, "ns", Publish, CancellationToken.None);
    }

    [Fact] void should_notify_the_partition_replay_began_then_ended() => _notifications.ShouldContainOnly("begin-partition:x", "end-partition:x");
    [Fact] void should_not_publish_a_result() => _published.ShouldBeEmpty();
}
