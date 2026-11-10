// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_the_client_declared_page_bounds : given.a_subscriber
{
    readonly List<AppendedEvent[]> _delivered = [];
    ObserverSubscriberResult _result;

    void Establish()
    {
        _mediator.GetPageDelivery(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(new PageDeliveryDefinition(MaxEventsPerPage: 2));
        _mediator.When(mediator => mediator.OnNext(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<TaskCompletionSource<ObserverSubscriberResult>>(), Arg.Any<TimeSpan?>(), Arg.Any<ObservationOrigin>()))
            .Do(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _delivered.Add(events);
                call.Arg<TaskCompletionSource<ObserverSubscriberResult>>().SetResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because() => _result = await _subscriber.OnNext(
        "partition",
        [AppendedEvent.EmptyWithEventSequenceNumber(1), AppendedEvent.EmptyWithEventSequenceNumber(2), AppendedEvent.EmptyWithEventSequenceNumber(3)],
        _context);

    [Fact] void should_deliver_two_pages() => _delivered.Count.ShouldEqual(2);
    [Fact] void should_keep_every_page_within_the_declared_bound() => _delivered.ShouldEachConformTo(page => page.Length <= 2);
    [Fact] void should_report_the_last_event_as_handled() => _result.LastSuccessfulObservation.Value.ShouldEqual(3UL);
}
