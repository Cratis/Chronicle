// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_delivery_fails_after_the_budget_is_confirmed : given.a_bounded_replay_job_step
{
    void Establish()
    {
        SetupPages(Events(1, 2, 3, 4, 5, 6, 7));
        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(call =>
        {
            var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
            if (events[0].Context.SequenceNumber == (EventSequenceNumber)4UL) throw new InvalidOperationException("delivery failed");
            return Observe(events);
        });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_persist_only_the_confirmed_prefix() => _jobStep.PersistedPositions.ShouldEqual(new EventSequenceNumber[] { 3 });
    [Fact] void should_report_no_failed_events() => _reports.SelectMany(report => report.Counts.Values).Sum(count => (int)count.Value).ShouldEqual(3);
    [Fact] void should_confirm_before_the_failed_delivery() => _durableAtReport.ShouldEqual(new EventSequenceNumber[] { 3 });
    [Fact] void should_not_dispatch_the_tail_after_failure() => _dispatches.SelectMany(events => events).Select(@event => @event.Context.SequenceNumber).ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
}
