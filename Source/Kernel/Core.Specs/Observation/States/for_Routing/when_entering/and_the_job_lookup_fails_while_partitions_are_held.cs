// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// Releasing on a failed lookup would bring the double delivery back on every transient storage error, so the holds stay.
/// </summary>
public class and_the_job_lookup_fails_while_partitions_are_held : given.a_routing_state_with_partition_holds
{
    void Establish() => _jobStorage
        .GetJobs(Arg.Any<JobQuery>())
        .Returns(_ => Task.FromResult(Catch<IImmutableList<JobState>>.Failed(new InvalidOperationException("The job storage is unavailable"))));

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_keep_the_catching_up_partition_held() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_catchingUpPartition);
    [Fact] void should_keep_the_replaying_partition_held() => _resultingStoredState.ReplayingPartitions.ShouldContain(_replayingPartition);
    [Fact] void should_still_evaluate_routing() => _observer.Received(1).CatchUp();
}
