// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_looking_outside_prepared_parent_history : given.a_bounded_parent_page
{
    readonly CancellationTokenSource _cancellation = new();
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _older;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _later;

    async Task Because()
    {
        await Prepare();
        _older = await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 2, 1, _cancellation.Token);
        _later = await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 21, 20, _cancellation.Token);
        await _storage.GetInstancesOfAnyInRange([_eventSourceId], [new("other-type")], 13, 12, _cancellation.Token);
        await _storage.GetInstancesOfAnyInRange([new("other-source")], _eventTypeIds, 13, 12, _cancellation.Token);
        await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 13, 12);
    }

    [Fact] void should_fetch_an_older_predecessor_on_demand() => _inner.Received(1).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.SequenceEqual(new[] { _eventSourceId })), _eventTypeIds, 2, 1, _cancellation.Token);
    [Fact] void should_forward_a_child_after_the_page_with_its_token() => _inner.Received(1).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.SequenceEqual(new[] { _eventSourceId })), _eventTypeIds, 21, 20, _cancellation.Token);
    [Fact] void should_return_the_inner_older_result() => ReferenceEquals(_older, _history).ShouldBeTrue();
    [Fact] void should_return_the_inner_later_result() => ReferenceEquals(_later, _history).ShouldBeTrue();
    [Fact] void should_not_share_entries_across_sources_or_types_or_replace_the_page() => _inner.Received(5).GetInstancesOfAnyInRange(
        Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());

    void Destroy() => _cancellation.Dispose();
}
