// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page;

/// <summary>
/// A page larger than the bound the client declared is reduced as several pages, one after another.
/// </summary>
public class and_the_client_declared_a_bound_smaller_than_the_page : given.a_page_subscriber
{
    void Establish() => _mediator.GetPageDelivery(Arg.Any<ReducerId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
        .Returns(new PageDeliveryDefinition(MaxEventsPerPage: 1));

    Task Because() => Deliver();

    [Fact] void should_send_two_pages_to_the_client() => _pagesSentToTheClient.Count.ShouldEqual(2);
    [Fact] void should_keep_every_page_within_the_bound() => _pagesSentToTheClient.ShouldEachConformTo(page => page.Sum(_ => _.Events.Count()) == 1);
    [Fact] void should_settle_the_first_partition() => ResultFor("first").State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_settle_the_second_partition() => ResultFor("second").State.ShouldEqual(ObserverSubscriberState.Ok);
}
