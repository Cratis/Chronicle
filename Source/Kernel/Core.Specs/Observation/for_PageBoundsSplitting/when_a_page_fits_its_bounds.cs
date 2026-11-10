// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PageBoundsSplitting;

public class when_a_page_fits_its_bounds : given.events
{
    AppendedEvent[] _events;
    IReadOnlyList<IReadOnlyList<AppendedEvent>> _pages;

    void Establish() => _events = [EventWith(1, 10), EventWith(2, 10)];

    void Because() => _pages = new PageBounds(2, PageBoundsSplitting.SizeOf(_events[0]) * 2).Split(_events);

    [Fact] void should_keep_one_page() => _pages.Single().ShouldContainOnly(_events);
}
