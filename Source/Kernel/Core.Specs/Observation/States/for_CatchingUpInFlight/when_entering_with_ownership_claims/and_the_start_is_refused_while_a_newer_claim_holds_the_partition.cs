// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_ownership_claims;

public class and_the_start_is_refused_while_a_newer_claim_holds_the_partition : given.a_catching_up_in_flight_state_with_ownership_claims
{
    void Establish()
    {
        _storedState = _storedState with { InFlightPartitions = new HashSet<Key> { _partition } };
        _jobsManager
            .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.NotAdmitted)));
        _claims.IsHeldByNewerClaim(_partition, 11UL).Returns(true);
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_quarantine_the_observer() => _stateMachine.DidNotReceive().TransitionTo<QuarantinedObserver>();
    [Fact] void should_go_on_routing() => _stateMachine.Received(1).TransitionTo<Routing>();
    [Fact] void should_remove_the_marker() => _resultingStoredState.InFlightPartitions.ShouldBeEmpty();
}
