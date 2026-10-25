// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// A stale observer-wide catch-up job owns nothing, so an ownerless hold cannot block delivery or replays forever (#5406).
/// </summary>
public class and_the_observer_wide_catch_up_job_is_stale : given.a_routing_state_with_partition_holds
{
    void Establish()
    {
        HasOutstandingJobs(StaleObserverCatchUpJob());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_release_the_catching_up_partition() => _resultingStoredState.CatchingUpPartitions.ShouldBeEmpty();
}
