// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_update_fails;

public class and_every_retry_fails : given.an_event_sequence_with_a_failing_index
{
    void Establish() => _update = _ => Task.FromException(new given.SimulatedStateWriteError());
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_succeed_despite_the_failed_index() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_attempt_the_event_three_times() => _attempts.Count.ShouldEqual(3);
    [Fact] void should_keep_the_watermark_below_the_failure() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
}
