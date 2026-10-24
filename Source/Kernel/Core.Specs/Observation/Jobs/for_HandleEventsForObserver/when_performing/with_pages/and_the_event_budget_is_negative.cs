// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_event_budget_is_negative : given.a_bounded_replay_job_step
{
    protected override int Interval => -1;
    protected override int PageCap => 2;

    void Establish() => SetupPages(Events(1, 2, 3, 4, 5, 6));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_only_apply_the_existing_page_cap() => _dispatches.Select(events => events.Length).ShouldEqual([2, 2, 2]);
    [Fact] void should_not_force_any_remainder() => _durableAtReport.TrueForAll(position => position == EventSequenceNumber.Unavailable).ShouldBeTrue();
    [Fact] void should_flush_at_completion() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)6UL);
}
