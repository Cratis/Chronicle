// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_a_controlled_replay_has_a_persisted_boundary : given.a_controlled_replay
{
    void Establish() => _stateStorage.State.ControlledReplayBoundary = 75UL;

    async Task Because() => resumed_steps = (await _job.PrepareStepsForTesting(request)).ToArray();

    [Fact] void should_use_the_saved_boundary_for_every_step() => resumed_steps.All(step => ((HandleEventsForPartitionArguments)step.Request).EndEventSequenceNumber == (EventSequenceNumber)75UL).ShouldBeTrue();
    [Fact] void should_prepare_all_partition_steps() => resumed_steps.Length.ShouldEqual(_keys.Length);
    [Fact] void should_not_read_a_new_tail() => _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>());
}
