// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching_a_page_to_a_batch_reactor;

public class and_the_batch_method_throws : given.a_batch_reactor_dispatcher
{
    void Establish() => _recorder.OnBatch = _ => throw new Exception("Index is down");

    Task Because() => Dispatch(Page(("first", [(1, "registered"), (2, "removed")]), ("second", [(3, "removed")])));

    [Fact] void should_fail_the_partition_at_its_first_batched_event() => First.State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_not_advance_the_partition_past_the_failure() => First.LastSuccessfulObservation.ShouldEqual(ulong.MaxValue);
    [Fact] void should_carry_the_reason() => First.ExceptionMessages.ShouldContain("Index is down");
    [Fact] void should_let_a_partition_without_batched_events_succeed() => Second.State.ShouldEqual(ObservationState.Success);
}
