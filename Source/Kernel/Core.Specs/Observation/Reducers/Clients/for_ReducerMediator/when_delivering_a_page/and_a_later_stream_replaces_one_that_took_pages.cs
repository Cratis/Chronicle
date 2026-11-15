// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.when_delivering_a_page;

/// <summary>
/// A stream that replaces an earlier one does not inherit the earlier stream's page delegate.
/// </summary>
public class and_a_later_stream_replaces_one_that_took_pages : given.a_page_to_deliver
{
    void Establish()
    {
        SubscribeEvents();
        SubscribePages();
        SubscribeEvents();
    }

    void Because() => DeliverThePage();

    [Fact] void should_not_deliver_to_the_earlier_streams_page_delegate() => _pageDeliveries.ShouldBeEmpty();
    [Fact] void should_deliver_each_partition_to_the_later_stream() => _singleDeliveries.Count.ShouldEqual(2);
}
