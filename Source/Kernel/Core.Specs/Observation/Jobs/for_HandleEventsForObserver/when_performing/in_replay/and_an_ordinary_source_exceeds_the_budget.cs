// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_an_ordinary_source_exceeds_the_budget : given.a_bounded_replay_job_step
{
    protected override bool UsesPages => false;

    void Establish() => SetupPages(Events(1, 2, 10, 11, 20, 21, 30));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_bound_each_delivery_by_event_count() => _dispatches.Select(events => events.Length).ShouldEqual([3, 3, 1]);
    [Fact] void should_confirm_the_first_three_events_before_dispatching_more() => _durableAtDispatch[1].ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_confirm_the_next_three_before_dispatching_the_remainder() => _durableAtDispatch[2].ShouldEqual((EventSequenceNumber)21UL);
    [Fact] void should_never_use_page_delivery() => _pageSubscriber.DidNotReceive().OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_report_every_successful_event_once() => _reports.SelectMany(report => report.Counts.Values).Sum(count => (int)count.Value).ShouldEqual(7);
}
