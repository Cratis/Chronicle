// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// A job that has reported back has handed its holds over, even while it is still listed as outstanding (#5406).
/// </summary>
public class and_the_observer_wide_catch_up_job_has_reported_back : given.a_routing_state_with_partition_holds
{
    void Establish()
    {
        var job = ObserverCatchUpJob();
        _isConcludedCatchUpJob = id => id == job.Id;
        HasOutstandingJobs(job);
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_release_the_catching_up_partition() => _resultingStoredState.CatchingUpPartitions.ShouldBeEmpty();
}
