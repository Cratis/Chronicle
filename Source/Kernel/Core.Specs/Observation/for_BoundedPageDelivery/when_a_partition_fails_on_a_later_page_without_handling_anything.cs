// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery;

public class when_a_partition_fails_on_a_later_page_without_handling_anything : given.a_page_beyond_its_bounds
{
    ObserverPageResult _result;

    async Task Because() => _result = await BoundedPageDelivery.Deliver(
        _bounds,
        _batches,
        page => Answer(page, batch => _delivered.Count > 1 && batch.Partition.Equals(_first) ? ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Failed") : HandledAll(batch)));

    [Fact] void should_report_the_failure() => _result.Results[_first].State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_keep_what_the_earlier_page_handled() => _result.Results[_first].LastSuccessfulObservation.Value.ShouldEqual(2UL);
}
