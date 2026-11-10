// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery;

public class when_delivering_a_page_within_its_bounds : given.a_page_beyond_its_bounds
{
    async Task Because() => await BoundedPageDelivery.Deliver(new PageBounds(10, int.MaxValue), _batches, page => Answer(page, HandledAll));

    [Fact] void should_deliver_it_as_it_is() => _delivered.Single().ShouldBeSame(_batches);
}
