// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// An outstanding observer-wide catch-up job owns the holds it registered, and only those (#5406).
/// </summary>
public class and_an_observer_wide_catch_up_job_is_outstanding : given.a_routing_state_with_partition_holds
{
    void Establish()
    {
        HasOutstandingJobs(ObserverCatchUpJob());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_keep_the_catching_up_partition_held() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_catchingUpPartition);
    [Fact] void should_release_the_replaying_partition_without_a_job() => _resultingStoredState.ReplayingPartitions.ShouldBeEmpty();
}
