// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_a_bound_exceeds_the_storage_ceiling : given.a_page_query
{
    const ulong BeyondCeiling = long.MaxValue + 1UL;
    readonly List<EventSequencePage> _pages = [];

    void Establish() => _cursor.MoveNext().Returns(false);

    async Task Because()
    {
        _pages.Add(await Query(from: BeyondCeiling, until: BeyondCeiling + 1));
        _pages.Add(await Query(until: BeyondCeiling));
        _pages.Add(await Query(from: BeyondCeiling, backward: true));
        _pages.Add(await Query(from: BeyondCeiling + 1, backward: true, until: BeyondCeiling));
    }

    [Fact] void should_accept_ordinary_unsigned_bounds() => _pages.Count.ShouldEqual(4);
    [Fact] void should_return_empty_pages_when_storage_has_no_matches() => _pages.TrueForAll(page => !page.Events.Any() && page.Next is null).ShouldBeTrue();
    [Fact] void should_pass_a_forward_start_to_the_storage_lower_bound() => _sequence.Received(1).GetWindow(BeyondCeiling, BeyondCeiling + 1, 3, false, null, null, null, null, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<IEnumerable<Tag>>());
    [Fact] void should_pass_a_forward_stop_to_the_storage_upper_bound() => _sequence.Received(1).GetWindow(0, BeyondCeiling, 3, false, null, null, null, null, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<IEnumerable<Tag>>());
    [Fact] void should_pass_a_backward_start_to_the_storage_upper_bound() => _sequence.Received(1).GetWindow(0, BeyondCeiling, 3, true, null, null, null, null, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<IEnumerable<Tag>>());
    [Fact] void should_pass_a_backward_stop_to_the_storage_lower_bound() => _sequence.Received(1).GetWindow(BeyondCeiling, BeyondCeiling + 1, 3, true, null, null, null, null, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<IEnumerable<Tag>>());
}
