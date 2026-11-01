// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_a_controlled_replay_is_prepared_again : given.a_controlled_replay
{
    async Task Because()
    {
        first_steps = (await _job.PrepareStepsForTesting(request)).ToArray();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)200UL);
        resumed_steps = (await _job.PrepareStepsForTesting(request)).ToArray();
    }

    [Fact] void should_capture_a_finite_boundary() => _stateStorage.State.ControlledReplayBoundary.ShouldEqual((EventSequenceNumber)100UL);
    [Fact] void should_bound_every_initial_step() => first_steps.All(step => ((HandleEventsForPartitionArguments)step.Request).EndEventSequenceNumber == (EventSequenceNumber)100UL).ShouldBeTrue();
    [Fact] void should_reuse_the_boundary_for_every_resumed_step() => resumed_steps.All(step => ((HandleEventsForPartitionArguments)step.Request).EndEventSequenceNumber == (EventSequenceNumber)100UL).ShouldBeTrue();
    [Fact] void should_have_nonempty_step_sets() => (first_steps.Length == _keys.Length && resumed_steps.Length == _keys.Length).ShouldBeTrue();
    [Fact] void should_read_the_tail_only_once() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>());
}
