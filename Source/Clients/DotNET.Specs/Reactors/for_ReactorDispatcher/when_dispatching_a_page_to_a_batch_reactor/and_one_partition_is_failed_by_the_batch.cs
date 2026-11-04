// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching_a_page_to_a_batch_reactor;

public class and_one_partition_is_failed_by_the_batch : given.a_batch_reactor_dispatcher
{
    void Establish() => _recorder.OnBatch = batch => batch.Fail(new EventSourceId("second"), new Exception("Index rejected it"));

    Task Because() => Dispatch(Page(("first", [(1, "registered"), (2, "removed")]), ("second", [(3, "registered")])));

    [Fact] void should_call_the_batch_method_once_with_its_events_in_order() => _recorder.Batched.ShouldContainOnly(1UL, 3UL);
    [Fact] void should_call_the_per_event_method_for_its_event() => _recorder.PerEvent.ShouldContainOnly(2UL);
    [Fact] void should_tell_the_batch_where_the_page_came_from() => _recorder.Context!.Origin.ShouldEqual(Observation.ObservationOrigin.Replay);
    [Fact] void should_let_the_other_partition_succeed() => First.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_advance_the_other_partition_to_its_last_event() => First.LastSuccessfulObservation.ShouldEqual(2UL);
    [Fact] void should_fail_the_partition() => Second.State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_not_advance_the_failed_partition() => Second.LastSuccessfulObservation.ShouldEqual(ulong.MaxValue);
    [Fact] void should_carry_the_reason() => Second.ExceptionMessages.ShouldContain("Index rejected it");
}
