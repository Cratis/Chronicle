// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_a_partition_registered_by_the_observer_catch_up;

/// <summary>
/// A partition catch-up claimed the partition and its durable job is outstanding, so that job owns it as it did before.
/// </summary>
public class and_a_partition_catch_up_job_owns_the_partition : given.a_partition_registered_by_the_observer_catch_up
{
    void Establish()
    {
        _storedState = _storedState with
        {
            PartitionClaims = [new(_partition, ClaimHolderKind.PartitionCatchUp, null, 3UL, 42UL, [])]
        };
        HasOutstandingJobs(
            new JobState
            {
                Id = _observerCatchUpJob,
                Status = JobStatus.Running,
                Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, 0UL, []) { OwnershipEpoch = WideClaimEpoch }
            },
            PartitionCatchUpJob(_partition));
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_start_a_partition_catch_up() => ShouldStartNoPartitionCatchUp();
    [Fact] void should_remove_the_marker() => _resultingStoredState.InFlightPartitions.ShouldBeEmpty();
}
