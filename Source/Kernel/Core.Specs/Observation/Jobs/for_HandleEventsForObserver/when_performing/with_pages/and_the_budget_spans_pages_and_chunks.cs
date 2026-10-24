// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_budget_spans_pages_and_chunks : given.a_bounded_replay_job_step
{
    protected override int Interval => 5;
    protected override int PageCap => 2;

    void Establish() => SetupPages(Events(1, 2), Events(3, 4, 5, 6));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_keep_the_budget_across_cursor_pages() => _dispatches.Select(events => events.Length).ShouldEqual([2, 2, 1, 1]);
    [Fact] void should_checkpoint_before_the_last_dispatch() => _durableAtDispatch[^1].ShouldEqual((EventSequenceNumber)5UL);
}
