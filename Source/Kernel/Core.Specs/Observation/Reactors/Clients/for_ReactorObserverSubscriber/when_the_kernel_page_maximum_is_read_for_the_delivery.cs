// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

/// <summary>
/// The kernel's page maximum is read from the observer's configuration for every delivery, so a changed setting applies
/// without restarting.
/// </summary>
public class when_the_kernel_page_maximum_is_read_for_the_delivery : given.a_subscriber
{
    readonly List<AppendedEvent[]> _delivered = [];
    IConfigurationForObserverProvider _configuration;

    void Establish()
    {
        _configuration = Substitute.For<IConfigurationForObserverProvider>();
        _configuration.GetFor(Arg.Any<string>()).Returns(new Observers { MaxEventsPerPage = 2 });
        _subscriber = new(_mediator, NullLogger<ReactorObserverSubscriber>.Instance, _clock, configurationProvider: _configuration);
        _mediator.GetPageDelivery(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(new PageDeliveryDefinition());
        _mediator.When(mediator => mediator.OnNext(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<TaskCompletionSource<ObserverSubscriberResult>>(), Arg.Any<TimeSpan?>(), Arg.Any<ObservationOrigin>()))
            .Do(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _delivered.Add(events);
                call.Arg<TaskCompletionSource<ObserverSubscriberResult>>().SetResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because() => await _subscriber.OnNext(
        "partition",
        [AppendedEvent.EmptyWithEventSequenceNumber(1), AppendedEvent.EmptyWithEventSequenceNumber(2), AppendedEvent.EmptyWithEventSequenceNumber(3)],
        _context);

    [Fact] void should_split_by_the_configured_maximum() => _delivered.Count.ShouldEqual(2);
    [Fact] void should_read_the_configuration_of_the_observer() => _ = _configuration.Received().GetFor(Arg.Any<string>());
}
