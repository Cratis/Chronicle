// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorMediator.when_getting_page_delivery;

public class and_the_connection_subscribed_to_pages : given.a_reactor_mediator
{
    readonly PageDeliveryDefinition _declared = new(MaxEventsPerPage: 10, MaxBytesPerPage: 1000);
    PageDeliveryDefinition? _result;

    void Establish()
    {
        _mediator.Subscribe(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), (_, _, _, _, _) => { });
        _mediator.SubscribePages(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), (_, _, _, _) => { }, _declared);
    }

    void Because() => _result = _mediator.GetPageDelivery(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"));

    [Fact] void should_return_the_declared_bounds() => _result.ShouldEqual(_declared);
}
