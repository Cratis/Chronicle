// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_the_controlled_sequence_is_empty : given.a_controlled_replay
{
    void Establish() => _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);

    async Task Because() => first_steps = (await _job.PrepareStepsForTesting(request)).ToArray();

    [Fact] void should_prepare_no_replay_reader() => first_steps.ShouldBeEmpty();
    [Fact] void should_persist_the_empty_boundary() => _stateStorage.State.ControlledReplayBoundary.ShouldEqual(EventSequenceNumber.Unavailable);
}
