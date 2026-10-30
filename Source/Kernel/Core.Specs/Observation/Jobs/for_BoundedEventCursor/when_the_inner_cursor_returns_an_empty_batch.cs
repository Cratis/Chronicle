// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_BoundedEventCursor;

public class when_the_inner_cursor_returns_an_empty_batch : given.a_cursor_over_batches
{
    protected override int Bound => 2;

    protected override IReadOnlyList<AppendedEvent[]> Batches => [Events()];

    bool _moved;

    async Task Because() => _moved = await _cursor.MoveNext();

    [Fact] void should_pass_it_through_as_a_slice() => _moved.ShouldBeTrue();
    [Fact] void should_have_no_events() => _cursor.Current.ShouldBeEmpty();
}
