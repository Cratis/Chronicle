// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_update_fails;

public class and_the_store_reports_a_duplicate_claim : given.an_event_sequence_with_a_failing_index
{
    void Establish() => _update = _ => Task.FromException(new DuplicateUniqueConstraintValue("failing", "value"));
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_retry_a_settled_claim() => _attempts.Count.ShouldEqual(1);
    [Fact] void should_advance_the_watermark() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(_appendedSequenceNumber.Next());
}
