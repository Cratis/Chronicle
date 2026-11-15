// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery;

public class when_a_partition_fails_part_way_through_its_pages : Specification
{
    int _deliveries;
    ObserverSubscriberResult _result;

    async Task Because() => _result = await BoundedPageDelivery.Deliver(
        new PageBounds(2, int.MaxValue),
        [.. Enumerable.Range(1, 5).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_))],
        events =>
        {
            var page = events.ToArray();
            return Task.FromResult(++_deliveries == 2
                ? ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Failed")
                : ObserverSubscriberResult.Ok(page[^1].Context.SequenceNumber));
        });

    [Fact] void should_stop_at_the_failing_page() => _deliveries.ShouldEqual(2);
    [Fact] void should_report_the_failure() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_keep_what_the_earlier_page_handled() => _result.LastSuccessfulObservation.Value.ShouldEqual(2UL);
}
