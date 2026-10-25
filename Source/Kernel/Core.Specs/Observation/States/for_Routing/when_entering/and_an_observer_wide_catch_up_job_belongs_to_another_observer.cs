// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// Another observer's job owns nothing here (#5406).
/// </summary>
public class and_an_observer_wide_catch_up_job_belongs_to_another_observer : given.a_routing_state_with_partition_holds
{
    void Establish()
    {
        HasOutstandingJobs(ObserverCatchUpJob(key: new(Guid.NewGuid().ToString(), _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId)));
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_release_the_catching_up_partition() => _resultingStoredState.CatchingUpPartitions.ShouldBeEmpty();
}
