// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Collections;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceCache;

public class when_getting_view_of_middle_part_of_cache : given.an_event_sequence_cache
{
    SortedSet<AppendedEvent> result;

    void Establish() => Enumerable.Range(0, 100).ForEach(_ => cache.Add(AppendedEvent.EmptyWithEventSequenceNumber((ulong)_)));

    void Because() => result = cache.GetView(50, 75);

    [Fact] void should_return_the_correct_number_of_events() => result.Count.ShouldEqual(26);
    [Fact] void should_return_the_correct_events() => result.Select(_ => _.Metadata.SequenceNumber.Value).ShouldEqual(Enumerable.Range(50, 26).Select(_ => (ulong)_));
}
