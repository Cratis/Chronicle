// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_cancelled_after_the_budget_is_handled : given.a_bounded_replay_job_step
{
    readonly CancellationTokenSource _cancellation = new();

    void Establish()
    {
        SetupPages(Events(1, 2, 3, 4, 5, 6));
        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(call =>
        {
            var result = Observe(call.Arg<IEnumerable<AppendedEvent>>());
            _cancellation.Cancel();
            return result;
        });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState, _cancellation.Token);

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_not_dispatch_past_the_confirmed_boundary() => _dispatches.SelectMany(events => events).Select(@event => @event.Context.SequenceNumber).ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
    [Fact] void should_persist_the_successful_prefix() => _jobStep.PersistedPositions.ShouldEqual(new EventSequenceNumber[] { 3 });
    [Fact] void should_report_the_prefix_after_it_is_durable() => _durableAtReport.ShouldEqual(new EventSequenceNumber[] { 3 });
}
