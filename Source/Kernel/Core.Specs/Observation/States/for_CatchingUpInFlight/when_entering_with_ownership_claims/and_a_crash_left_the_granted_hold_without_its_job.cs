// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_ownership_claims;

/// <summary>
/// The hold and claim are persisted before the job starts. A crash in between leaves the partition held by no job, and
/// its in-flight marker is then the only record of the recovery it still owes.
/// </summary>
public class and_a_crash_left_the_granted_hold_without_its_job : given.a_catching_up_in_flight_state_with_ownership_claims
{
    void Establish() => _storedState = _storedState with
    {
        LastHandledEventSequenceNumber = 41UL,
        InFlightPartitions = new HashSet<Key> { _partition },
        CatchingUpPartitions = new HashSet<Key> { _partition }
    };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_grant_the_claim_again() => _claims.Received(1)
        .GrantPartitionsBeforeStart(Arg.Is<IEnumerable<Key>>(_ => _.Single() == _partition), ClaimHolderKind.PartitionCatchUp, (EventSequenceNumber)42UL);
    [Fact] void should_restart_the_catch_up_from_the_saved_position() => _started.Single().FromSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_start_it_with_the_new_epoch() => _started.Single().OwnershipEpoch.ShouldEqual(11UL);
    [Fact] void should_keep_the_partition_held() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_remove_the_marker_once_the_job_is_durable() => _resultingStoredState.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_go_on_routing() => _stateMachine.Received(1).TransitionTo<Routing>();
}
