// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

/// <summary>
/// The pages of one delivery share its deadline. A page answered after the deadline has passed is discarded, no later page is
/// sent, and the progress of the pages answered in time is kept, so only the rest is delivered again.
/// </summary>
public class when_the_deadline_passes_part_way_through_the_pages : given.a_subscriber
{
    int _pagesSent;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _mediator.GetPageDelivery(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(new PageDeliveryDefinition(MaxEventsPerPage: 2));
        _mediator.When(mediator => mediator.OnNext(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<TaskCompletionSource<ObserverSubscriberResult>>(), Arg.Any<TimeSpan?>(), Arg.Any<ObservationOrigin>()))
            .Do(call =>
            {
                _pagesSent++;

                // Each page takes twenty of the delivery's thirty seconds, so the second is answered after the deadline.
                _clock.Advance(TimeSpan.FromSeconds(20));
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                call.Arg<TaskCompletionSource<ObserverSubscriberResult>>().SetResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because() => _result = await _subscriber.OnNext(
        "partition",
        [.. Enumerable.Range(1, 5).Select(sequenceNumber => AppendedEvent.EmptyWithEventSequenceNumber((ulong)sequenceNumber))],
        _context);

    [Fact] void should_not_send_a_page_after_the_late_one() => _pagesSent.ShouldEqual(2);
    [Fact] void should_report_the_delivery_as_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_keep_what_the_page_answered_in_time_handled() => _result.LastSuccessfulObservation.Value.ShouldEqual(2UL);
}
