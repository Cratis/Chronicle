// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_chunks_exhaust_the_remaining_budget : given.a_bounded_replay_job_step
{
    protected override int Interval => 5;
    protected override int PageCap => 2;

    void Establish() => SetupPages(Events(1, 2, 3, 4, 5, 6));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_recompute_the_limit_after_each_chunk() => _dispatches.Select(events => events.Length).ShouldEqual([2, 2, 1, 1]);
    [Fact] void should_confirm_five_before_dispatching_six() => _durableAtDispatch[^1].ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_preserve_order() => _dispatches.SelectMany(events => events).Select(@event => @event.Context.SequenceNumber).ShouldEqual(new EventSequenceNumber[] { 1, 2, 3, 4, 5, 6 });
}
