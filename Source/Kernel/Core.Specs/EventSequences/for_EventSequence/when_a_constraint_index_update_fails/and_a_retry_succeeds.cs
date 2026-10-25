// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_update_fails;

public class and_a_retry_succeeds : given.an_event_sequence_with_a_failing_index
{
    void Establish()
    {
        UseDeterministicRetryClock();
        _update = _ => _attempts.Count == 1 ? Task.FromException(new given.SimulatedStateWriteError()) : Task.CompletedTask;
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_retry_the_same_event_once() => _attempts.ShouldEqual(new[] { _appendedSequenceNumber, _appendedSequenceNumber });
    [Fact] void should_advance_the_watermark() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(_appendedSequenceNumber.Next());
}
