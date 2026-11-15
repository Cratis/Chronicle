// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.given;

/// <summary>
/// The observer-wide catch-up registered an in-flight partition as catching up. Its claim resumes at 42, where a new
/// partition catch-up would start.
/// </summary>
public class a_partition_registered_by_the_observer_catch_up : a_catching_up_in_flight_state_with_ownership_claims
{
    protected const ulong WideClaimEpoch = 7;
    protected JobId _observerCatchUpJob;

    void Establish()
    {
        _observerCatchUpJob = JobId.New();
        _storedState = _storedState with
        {
            LastHandledEventSequenceNumber = 41UL,
            InFlightPartitions = new HashSet<Key> { _partition },
            CatchingUpPartitions = new HashSet<Key> { _partition },
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, _observerCatchUpJob, WideClaimEpoch, 42UL, [])
        };
        ObserverCatchUpIsOutstandingWith(WideClaimEpoch);
    }

    protected void ObserverCatchUpIsOutstandingWith(ulong epoch) => HasOutstandingJobs(new JobState
    {
        Id = _observerCatchUpJob,
        Status = JobStatus.Running,
        Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []) { OwnershipEpoch = epoch }
    });

    protected void ShouldStartNoPartitionCatchUp() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
}
