// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_reading_forward_with_lookahead : given.a_page_query
{
    void Establish() => _window = [EventAt(4), EventAt(8), EventAt(20)];

    async Task Because() => _page = await Query(from: 3, until: 30);

    [Fact] void should_return_events_in_read_order() => _page.Events.Select(@event => @event.Id).ShouldEqual(["4", "8"]);
    [Fact] void should_continue_after_the_last_returned_event_not_the_lookahead() => _page.Next.ShouldEqual((ulong?)9);
    [Fact] void should_release_only_returned_content() => _released.Select(@event => @event.Context.SequenceNumber.Value).ShouldEqual([4UL, 8UL]);
    [Fact] void should_read_only_one_extra_matching_event() => _sequence.Received(1).GetWindow(3, 30, 3, false, null, null, null, null, Arg.Is<IEnumerable<Concepts.Events.EventType>>(_ => !_.Any()), Arg.Is<IEnumerable<Tag>>(_ => !_.Any()));
    [Fact] void should_dispose_the_cursor() => _cursor.Received(1).Dispose();
}
