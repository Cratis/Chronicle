// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_job_start_returns_an_error_partway_through : given.a_catching_up_in_flight_state
{
    readonly Key _firstPartition = "partition-one";
    readonly Key _secondPartition = "partition-two";
    readonly Key _thirdPartition = "partition-three";
    readonly List<Key> _attemptedPartitions = [];

    void Establish()
    {
        _storedState = _storedState with
        {
            InFlightPartitions = new HashSet<Key> { _firstPartition, _secondPartition, _thirdPartition }
        };

        // Fail the second call, regardless of HashSet enumeration order.
        _jobsManager
            .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(call =>
            {
                _attemptedPartitions.Add(call.Arg<CatchUpObserverPartitionRequest>().Key);
                var result = _attemptedPartitions.Count == 2
                    ? Result<JobId, StartJobError>.Failed(StartJobError.Unknown)
                    : Result<JobId, StartJobError>.Success(JobId.New());
                return Task.FromResult(result);
            });
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_stop_starting_jobs_after_the_failure() => _attemptedPartitions.Count.ShouldEqual(2);
    [Fact] void should_only_record_attempted_partitions_as_catching_up() => _resultingStoredState.CatchingUpPartitions.ShouldContainOnly(_attemptedPartitions);
    [Fact] void should_transition_to_quarantined() => _stateMachine.Received(1).TransitionTo<QuarantinedObserver>();
    [Fact] void should_not_transition_to_routing() => _stateMachine.DidNotReceive().TransitionTo<Routing>();
}
