// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently;

public class and_a_replay_begins_while_a_partition_is_running : given.a_delegate_with_gated_batches
{
    async Task Because()
    {
        Deliver("order-1", 1, 101);
        DeliverReplayState(ReplayState.BeginReplay, string.Empty);
        Deliver("order-2", 2, 102);
        DeliverReplayState(ReplayState.EndReplay, string.Empty);
        Deliver("order-3", 3, 103);
        Release("order-2", 2);
        Release("order-3", 3);
        await Started("order-1", 1);
        Release("order-1", 1);
        await Answered(103);
    }

    [Fact] void should_hold_back_work_after_the_replay_began_until_the_running_partition_is_done() => IndexOf("order-2:2:start").ShouldBeGreaterThan(IndexOf("order-1:1:end"));
    [Fact] void should_hold_back_work_after_the_replay_ended_until_earlier_work_is_done() => IndexOf("order-3:3:start").ShouldBeGreaterThan(IndexOf("order-2:2:end"));
}
