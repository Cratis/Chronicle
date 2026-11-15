// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.when_delivering_a_page;

public class and_the_client_does_not_take_pages : given.a_page_to_deliver
{
    void Establish() => SubscribeEvents();

    void Because() => DeliverThePage();

    [Fact] void should_deliver_each_partition_on_its_own() => _singleDeliveries.Select(_ => _.Partition.Value).ShouldContainOnly("first", "second");
}
