// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_ownership_claims;

public class and_a_partition_is_in_flight : given.a_catching_up_in_flight_state_with_ownership_claims
{
    void Establish() => _storedState = _storedState with { InFlightPartitions = new HashSet<Key> { _partition } };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_the_job_with_the_granted_epoch() => _started.Single().OwnershipEpoch.ShouldEqual(11UL);
    [Fact] void should_grant_the_claim_before_starting_the_job() => Received.InOrder(() =>
    {
        _claims.GrantPartitionsBeforeStart(Arg.Any<IEnumerable<Key>>(), ClaimHolderKind.PartitionCatchUp, Arg.Any<EventSequenceNumber>());
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    });
    [Fact] void should_remove_the_marker() => _resultingStoredState.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_go_on_routing() => _stateMachine.Received(1).TransitionTo<Routing>();
}
