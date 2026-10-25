// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_reading_backward_from_the_tail : given.a_page_query
{
    void Establish() => _window = [EventAt(20), EventAt(8), EventAt(0)];

    async Task Because() => _page = await Query(backward: true, until: 0);

    [Fact] void should_preserve_newest_first_order() => _page.Events.Select(@event => @event.Id).ShouldEqual(["20", "8"]);
    [Fact] void should_continue_before_the_last_returned_event() => _page.Next.ShouldEqual((ulong?)7);
    [Fact] void should_read_from_the_tail_without_first_fetching_it() => _sequence.Received(1).GetWindow(EventSequenceNumber.First, EventSequenceNumber.Max, 3, true, null, null, null, null, Arg.Is<IEnumerable<Concepts.Events.EventType>>(_ => !_.Any()), Arg.Is<IEnumerable<Tag>>(_ => !_.Any()));
}
