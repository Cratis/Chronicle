// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery;

public class when_delivering_a_partition_beyond_its_bounds : Specification
{
    readonly List<AppendedEvent[]> _delivered = [];
    ObserverSubscriberResult _result;

    async Task Because() => _result = await BoundedPageDelivery.Deliver(
        new PageBounds(2, int.MaxValue),
        [.. Enumerable.Range(1, 5).Select(_ => AppendedEvent.EmptyWithEventSequenceNumber((ulong)_))],
        events =>
        {
            var page = events.ToArray();
            _delivered.Add(page);
            return Task.FromResult(ObserverSubscriberResult.Ok(page[^1].Context.SequenceNumber));
        });

    [Fact] void should_deliver_three_pages() => _delivered.Count.ShouldEqual(3);
    [Fact] void should_deliver_the_first_two_events_first() => SequenceNumbersOf(0).ShouldEqual("1,2");
    [Fact] void should_deliver_the_next_two_events_second() => SequenceNumbersOf(1).ShouldEqual("3,4");
    [Fact] void should_deliver_the_last_event_alone() => SequenceNumbersOf(2).ShouldEqual("5");
    [Fact] void should_report_the_last_event_as_handled() => _result.LastSuccessfulObservation.Value.ShouldEqual(5UL);

    string SequenceNumbersOf(int page) => string.Join(',', _delivered[page].Select(_ => _.Context.SequenceNumber.Value));
}
