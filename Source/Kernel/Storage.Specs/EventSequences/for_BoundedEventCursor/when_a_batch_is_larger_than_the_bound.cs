// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.for_BoundedEventCursor;

public class when_a_batch_is_larger_than_the_bound : Specification
{
    BoundedEventCursor _cursor;
    List<ulong[]> _batches;

    void Establish() => _cursor = new(new MaterializedEventCursor(given.events.Numbered(1, 7)), 3);

    async Task Because() => _batches = await given.events.Walk(_cursor);

    [Fact] void should_split_it_into_bounded_batches() => _batches.Select(_ => _.Length).ShouldContainOnly(3, 3, 1);
    [Fact] void should_keep_every_event_in_order() => _batches.SelectMany(_ => _).ShouldContainOnly(1UL, 2UL, 3UL, 4UL, 5UL, 6UL, 7UL);
    [Fact] void should_start_with_the_first_event() => _batches[0][0].ShouldEqual(1UL);
    [Fact] void should_end_with_the_last_event() => _batches[^1][^1].ShouldEqual(7UL);
    [Fact] void should_have_no_current_events_at_the_end() => _cursor.Current.ShouldBeEmpty();
}
