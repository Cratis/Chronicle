// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching_a_page_to_a_batch_reactor;

public class and_one_partition_is_deferred_by_the_batch : given.a_batch_reactor_dispatcher
{
    void Establish() => _recorder.OnBatch = batch => batch.Defer(new EventSourceId("second"), TimeSpan.FromSeconds(30));

    Task Because() => Dispatch(Page(("first", [(1, "registered")]), ("second", [(3, "registered")])));

    [Fact] void should_defer_the_partition() => Second.State.ShouldEqual(ObservationState.Deferred);
    [Fact] void should_ask_for_the_delay() => Second.RetryAfterMilliseconds.ShouldEqual(30_000UL);
    [Fact] void should_let_the_other_partition_succeed() => First.State.ShouldEqual(ObservationState.Success);
}
