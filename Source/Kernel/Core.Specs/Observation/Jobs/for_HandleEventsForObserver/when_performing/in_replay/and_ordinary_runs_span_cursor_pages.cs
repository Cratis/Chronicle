// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_ordinary_runs_span_cursor_pages : given.a_bounded_replay_job_step
{
    protected override bool UsesPages => false;

    void Establish() => SetupPages(Events(1, 2), Events(3, 4), Events(5, 6));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_split_the_ordinary_runs_at_the_cumulative_budget() => _dispatches.Select(events => events.Length).ShouldEqual([2, 1, 1, 2]);
    [Fact] void should_confirm_before_delivering_event_four() => _durableAtDispatch[2].ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_confirm_the_second_interval() => _durableAtReport[^1].ShouldEqual((EventSequenceNumber)6UL);
}
