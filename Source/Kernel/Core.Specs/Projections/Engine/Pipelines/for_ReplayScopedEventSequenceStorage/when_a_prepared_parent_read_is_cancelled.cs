// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_a_prepared_parent_read_is_cancelled : given.a_bounded_parent_page
{
    Exception _error;
    readonly CancellationTokenSource _cancellation = new();

    async Task Because()
    {
        await Prepare();
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(() => _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 13, 12, _cancellation.Token));
    }

    [Fact] void should_honor_cancellation_even_on_a_cache_hit() => _error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_issue_another_storage_read() => _inner.Received(1).GetInstancesOfAnyInRange(
        Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());

    void Destroy() => _cancellation.Dispose();
}
