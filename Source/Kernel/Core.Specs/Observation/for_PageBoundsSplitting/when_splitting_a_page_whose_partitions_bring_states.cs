// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PageBoundsSplitting;

/// <summary>
/// A reducer page holds the state of every partition in it, so each state counts toward the byte bound once per page.
/// </summary>
public class when_splitting_a_page_whose_partitions_bring_states : given.events
{
    AppendedEvent[] _events;
    int _eventSize;
    IReadOnlyList<IReadOnlyList<ObserverPartitionBatch>> _pages;

    void Establish()
    {
        _events = [EventWith(1, 100), EventWith(2, 100), EventWith(3, 100)];
        _eventSize = PageBoundsSplitting.SizeOf(_events[0]);
    }

    /// <summary>
    /// Splits three events that fit the bound on their own, but of which only two fit with the states of their partitions.
    /// </summary>
    void Because() => _pages = new PageBounds(int.MaxValue, (_eventSize * 3) + 10).Split(
        [
            new ObserverPartitionBatch("first", [_events[0]]),
            new ObserverPartitionBatch("second", [_events[1]]),
            new ObserverPartitionBatch("third", [_events[2]])
        ],
        _ => _eventSize / 2);

    [Fact] void should_make_two_pages() => _pages.Count.ShouldEqual(2);
    [Fact] void should_fit_two_partitions_with_their_states_on_the_first_page() => _pages[0].Select(_ => _.Partition.Value.ToString()).ShouldContainOnly("first", "second");
    [Fact] void should_put_the_partition_beyond_the_bound_on_the_second_page() => _pages[1].Single().Partition.ShouldEqual((Concepts.Keys.Key)"third");
}
