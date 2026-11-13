// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PageBoundsSplitting;

public class when_splitting_a_page_by_its_event_bound : given.events
{
    IReadOnlyList<IReadOnlyList<ObserverPartitionBatch>> _pages;

    void Because() => _pages = new PageBounds(2, int.MaxValue).Split(
    [
        new ObserverPartitionBatch("first", [AppendedEvent.EmptyWithEventSequenceNumber(1), AppendedEvent.EmptyWithEventSequenceNumber(2), AppendedEvent.EmptyWithEventSequenceNumber(3)]),
        new ObserverPartitionBatch("second", [AppendedEvent.EmptyWithEventSequenceNumber(4)])
    ]);

    [Fact] void should_make_two_pages() => _pages.Count.ShouldEqual(2);
    [Fact] void should_fill_the_first_page_with_the_first_partition() => _pages[0].Single().Events.Select(_ => _.Context.SequenceNumber.Value).ShouldContainOnly(1UL, 2UL);
    [Fact] void should_continue_the_first_partition_on_the_second_page() => _pages[1][0].Partition.ShouldEqual((Concepts.Keys.Key)"first");
    [Fact] void should_put_the_rest_of_the_first_partition_on_the_second_page() => _pages[1][0].Events.Single().Context.SequenceNumber.Value.ShouldEqual(3UL);
    [Fact] void should_put_the_second_partition_on_the_second_page() => _pages[1][1].Events.Single().Context.SequenceNumber.Value.ShouldEqual(4UL);
}
