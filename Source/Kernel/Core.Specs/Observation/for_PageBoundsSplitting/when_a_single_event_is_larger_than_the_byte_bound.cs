// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PageBoundsSplitting;

public class when_a_single_event_is_larger_than_the_byte_bound : given.events
{
    AppendedEvent[] _events;
    IReadOnlyList<IReadOnlyList<AppendedEvent>> _pages;

    void Establish() => _events = [EventWith(1, 10), EventWith(2, 10_000), EventWith(3, 10)];

    void Because() => _pages = new PageBounds(int.MaxValue, PageBoundsSplitting.SizeOf(_events[0]) * 2).Split(_events);

    [Fact] void should_make_three_pages() => _pages.Count.ShouldEqual(3);
    [Fact] void should_send_the_large_event_alone() => _pages[1].Single().ShouldEqual(_events[1]);
    [Fact] void should_send_the_events_around_it() => _pages[0].Concat(_pages[2]).ShouldContainOnly(_events[0], _events[2]);
}
