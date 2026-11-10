// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorMediator.when_getting_page_delivery;

public class and_a_new_stream_replaced_the_page_subscription : given.a_reactor_mediator
{
    PageDeliveryDefinition? _result;

    void Establish()
    {
        _mediator.Subscribe(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), (_, _, _, _, _) => { });
        _mediator.SubscribePages(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), (_, _, _, _) => { }, new(MaxEventsPerPage: 10));
        _mediator.Subscribe(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"), (_, _, _, _, _) => { });
    }

    void Because() => _result = _mediator.GetPageDelivery(new ReactorId("reactor"), new ConnectionId("connection"), new EventStoreName("store"), new EventStoreNamespaceName("namespace"));

    [Fact] void should_not_have_page_delivery() => _result.ShouldBeNull();
}
