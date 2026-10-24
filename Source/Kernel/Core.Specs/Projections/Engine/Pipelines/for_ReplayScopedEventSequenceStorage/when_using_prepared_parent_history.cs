// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_using_prepared_parent_history : given.a_bounded_parent_page
{
    IReadOnlyList<AppendedEvent> _early;
    IReadOnlyList<AppendedEvent> _later;
    IReadOnlyList<AppendedEvent> _inclusive;
    IReadOnlyList<AppendedEvent> _upperBound;
    IReadOnlyList<AppendedEvent> _strict;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _miss;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _missAgain;

    async Task Because()
    {
        await Prepare();
        _early = (await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 10, 9))[_eventSourceId];
        _later = (await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 13, 12))[_eventSourceId];
        _inclusive = (await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 12, 12))[_eventSourceId];
        _strict = (await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 12, 11))[_eventSourceId];
        _upperBound = (await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 20, 19))[_eventSourceId];
        _miss = await _storage.GetInstancesOfAnyInRange([_missing], _eventTypeIds, 11, 10);
        _missAgain = await _storage.GetInstancesOfAnyInRange([_missing], _eventTypeIds, 19, 18);
    }

    [Fact] void should_return_the_predecessor_for_the_first_child() => _early.ShouldContainOnly(_predecessor);
    [Fact] void should_return_the_changed_key_for_a_later_child() => _later.ShouldContainOnly(_changed);
    [Fact] void should_allow_the_releasing_parent_at_an_inclusive_bound() => _inclusive.ShouldContainOnly(_predecessor, _changed);
    [Fact] void should_exclude_a_parent_at_the_strict_child_bound() => _strict.ShouldContainOnly(_predecessor);
    [Fact] void should_not_use_a_parent_at_the_page_upper_bound_for_that_child() => _upperBound.ShouldContainOnly(_changed);
    [Fact] void should_cache_an_omitted_source_as_a_miss() => _miss.Count.ShouldEqual(0);
    [Fact] void should_reuse_the_cached_miss_at_another_child_boundary() => _missAgain.Count.ShouldEqual(0);
    [Fact] void should_only_read_the_prepared_batch() => _inner.Received(1).GetInstancesOfAnyInRange(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());
}
