// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.when_delivering_a_page;

public class and_the_client_takes_pages : given.a_page_to_deliver
{
    void Establish()
    {
        SubscribeEvents();
        SubscribePages();
    }

    void Because() => DeliverThePage();

    [Fact] void should_deliver_the_whole_page_at_once() => _pageDeliveries.Single().ShouldEqual(_operations);
    [Fact] void should_not_deliver_any_partition_on_its_own() => _singleDeliveries.ShouldBeEmpty();
}
