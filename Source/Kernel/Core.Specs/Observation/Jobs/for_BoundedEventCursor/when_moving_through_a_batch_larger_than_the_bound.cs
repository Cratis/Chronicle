// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_BoundedEventCursor;

public class when_moving_through_a_batch_larger_than_the_bound : given.a_cursor_over_batches
{
    protected override int Bound => 2;

    protected override IReadOnlyList<AppendedEvent[]> Batches => [Events(0, 1, 2, 3, 4)];

    readonly List<ulong[]> _slices = [];

    async Task Because()
    {
        while (await _cursor.MoveNext())
        {
            _slices.Add(Numbers(_cursor.Current));
        }
    }

    [Fact] void should_deliver_three_slices() => _slices.Count.ShouldEqual(3);
    [Fact] void should_fill_the_first_slice_to_the_bound() => Joined(_slices[0]).ShouldEqual("0, 1");
    [Fact] void should_fill_the_second_slice_to_the_bound() => Joined(_slices[1]).ShouldEqual("2, 3");
    [Fact] void should_deliver_the_remainder_last() => Joined(_slices[2]).ShouldEqual("4");
    [Fact] void should_move_the_inner_cursor_once_per_inner_batch_plus_the_end() => _innerMoveNextCalls.ShouldEqual(2);
}
