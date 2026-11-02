// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_update_fails;

public class and_two_attempts_are_configured : given.an_event_sequence_with_a_failing_index
{
    void Establish()
    {
        _eventSequence.ConstraintIndexRepair = new() { UpdateAttempts = 2 };
        _update = _ => Task.FromException(new given.SimulatedStateWriteError());
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_attempt_the_event_twice() => _attempts.Count.ShouldEqual(2);
    [Fact] void should_still_succeed() => _result.IsSuccess.ShouldBeTrue();
}
