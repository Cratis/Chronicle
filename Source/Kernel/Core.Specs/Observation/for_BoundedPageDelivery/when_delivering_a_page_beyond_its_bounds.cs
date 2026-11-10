// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery;

public class when_delivering_a_page_beyond_its_bounds : given.a_page_beyond_its_bounds
{
    ObserverPageResult _result;

    async Task Because() => _result = await BoundedPageDelivery.Deliver(_bounds, _batches, page => Answer(page, HandledAll));

    [Fact] void should_deliver_two_pages() => _delivered.Count.ShouldEqual(2);
    [Fact] void should_keep_every_page_within_the_bound() => _delivered.ShouldEachConformTo(page => page.Sum(_ => _.Events.Count) <= 2);
    [Fact] void should_settle_the_partition_spanning_pages_at_its_last_event() => _result.Results[_first].LastSuccessfulObservation.Value.ShouldEqual(3UL);
    [Fact] void should_settle_the_other_partition() => _result.Results[_second].State.ShouldEqual(ObserverSubscriberState.Ok);
}
