// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery;

public class when_a_partition_fails_on_an_earlier_page : given.a_page_beyond_its_bounds
{
    ObserverPageResult _result;

    async Task Because() => _result = await BoundedPageDelivery.Deliver(
        _bounds,
        _batches,
        page => Answer(page, batch => batch.Partition.Equals(_first) ? ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Failed") : HandledAll(batch)));

    [Fact] void should_not_deliver_the_rest_of_the_failed_partition() => _delivered[1].ShouldEachConformTo(batch => !batch.Partition.Equals(_first));
    [Fact] void should_report_the_failure() => _result.Results[_first].State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_still_deliver_the_other_partition() => _result.Results[_second].State.ShouldEqual(ObserverSubscriberState.Ok);
}
