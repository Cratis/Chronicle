// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_ownership_claims;

/// <summary>
/// A failed lookup proves no job owns the partition, nor that one does: the marker stays for the next activation.
/// </summary>
public class and_the_jobs_owning_the_held_partition_cannot_be_looked_up : given.a_catching_up_in_flight_state_with_ownership_claims
{
    void Establish()
    {
        _storedState = _storedState with
        {
            InFlightPartitions = new HashSet<Key> { _partition },
            CatchingUpPartitions = new HashSet<Key> { _partition }
        };
        _jobStorage.GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(Catch<IImmutableList<JobState>>.Failed(new InvalidOperationException("Job storage is unavailable"))));
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_keep_the_marker() => _resultingStoredState.InFlightPartitions.ShouldContain(_partition);
    [Fact] void should_not_start_a_job() => _started.ShouldBeEmpty();
    [Fact] void should_go_on_routing() => _stateMachine.Received(1).TransitionTo<Routing>();
}
