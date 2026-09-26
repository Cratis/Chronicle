// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_job_start_throws : given.a_catching_up_in_flight_state
{
    readonly Key _partition = "partition-one";

    void Establish()
    {
        _storedState = _storedState with { InFlightPartitions = new HashSet<Key> { _partition } };
        _jobsManager
            .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .ThrowsAsync(new InvalidOperationException("job start failed"));
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_transition_to_quarantined() => _stateMachine.Received(1).TransitionTo<QuarantinedObserver>();
    [Fact] void should_not_transition_to_routing() => _stateMachine.DidNotReceive().TransitionTo<Routing>();
    [Fact] void should_record_the_partition_as_catching_up() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_partition);
}
