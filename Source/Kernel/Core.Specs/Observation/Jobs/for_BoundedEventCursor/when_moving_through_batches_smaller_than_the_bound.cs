// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_BoundedEventCursor;

public class when_moving_through_batches_smaller_than_the_bound : given.a_cursor_over_batches
{
    protected override int Bound => 5;

    protected override IReadOnlyList<AppendedEvent[]> Batches => [Events(0, 1), Events(2)];

    readonly List<ulong[]> _slices = [];

    async Task Because()
    {
        while (await _cursor.MoveNext())
        {
            _slices.Add(Numbers(_cursor.Current));
        }
    }

    [Fact] void should_pass_each_batch_through_as_one_slice() => _slices.Count.ShouldEqual(2);
    [Fact] void should_keep_the_first_batch_unchanged() => Joined(_slices[0]).ShouldEqual("0, 1");
    [Fact] void should_keep_the_second_batch_unchanged() => Joined(_slices[1]).ShouldEqual("2");
}
