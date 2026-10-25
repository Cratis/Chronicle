// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_a_parent_first_appears_inside_the_prepared_page : given.a_bounded_parent_page
{
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _before;
    IReadOnlyList<AppendedEvent> _after;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _beforeAgain;

    void Establish() => _inner.GetInstancesOfAnyInRange(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>())
        .Returns(new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>> { [_eventSourceId] = [_changed] });

    async Task Because()
    {
        await Prepare();
        _before = await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 10, 9);
        _after = (await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 13, 12))[_eventSourceId];
        _beforeAgain = await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 10, 9);
    }

    [Fact] void should_not_use_a_future_parent_for_the_earlier_child() => _before.Count.ShouldEqual(0);
    [Fact] void should_resolve_a_later_child_from_the_same_window() => _after.ShouldContainOnly(_changed);
    [Fact] void should_not_reuse_the_later_hit_for_an_earlier_child() => _beforeAgain.Count.ShouldEqual(0);
    [Fact] void should_only_read_one_batch_for_hits_and_misses() => _inner.Received(1).GetInstancesOfAnyInRange(
        Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());
}
