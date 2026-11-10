// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PageBoundsSplitting;

public class when_splitting_a_page_by_its_byte_bound : given.events
{
    AppendedEvent[] _events;
    IReadOnlyList<IReadOnlyList<ObserverPartitionBatch>> _pages;

    void Establish() => _events = [EventWith(1, 100), EventWith(2, 100), EventWith(3, 100)];

    void Because() => _pages = new PageBounds(int.MaxValue, PageBoundsSplitting.SizeOf(_events[0]) * 2).Split(
    [
        new ObserverPartitionBatch("first", [_events[0]]),
        new ObserverPartitionBatch("second", [_events[1]]),
        new ObserverPartitionBatch("third", [_events[2]])
    ]);

    [Fact] void should_make_two_pages() => _pages.Count.ShouldEqual(2);
    [Fact] void should_fit_two_events_on_the_first_page() => _pages[0].Select(_ => _.Partition.Value.ToString()).ShouldContainOnly("first", "second");
    [Fact] void should_put_the_event_beyond_the_bound_on_the_second_page() => _pages[1].Single().Partition.ShouldEqual((Concepts.Keys.Key)"third");
}
