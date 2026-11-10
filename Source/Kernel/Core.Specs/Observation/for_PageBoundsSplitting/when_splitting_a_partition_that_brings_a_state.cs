// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PageBoundsSplitting;

/// <summary>
/// A partition split across pages is reduced from its state in every page, so the state counts toward each page's byte bound.
/// </summary>
public class when_splitting_a_partition_that_brings_a_state : given.events
{
    AppendedEvent[] _events;
    IReadOnlyList<IReadOnlyList<AppendedEvent>> _pages;

    void Establish() => _events = [EventWith(1, 100), EventWith(2, 100), EventWith(3, 100), EventWith(4, 100)];

    void Because()
    {
        var eventSize = PageBoundsSplitting.SizeOf(_events[0]);
        _pages = new PageBounds(int.MaxValue, (eventSize * 3) + 10).Split(_events, eventSize);
    }

    [Fact] void should_make_two_pages() => _pages.Count.ShouldEqual(2);
    [Fact] void should_fit_two_events_beside_the_state_on_the_first_page() => _pages[0].Count.ShouldEqual(2);
    [Fact] void should_fit_two_events_beside_the_state_on_the_second_page() => _pages[1].Count.ShouldEqual(2);
}
