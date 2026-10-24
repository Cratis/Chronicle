// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_forwarding_bounded_parent_history : given.a_replay_scoped_storage
{
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _expected;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _result;
    readonly CancellationTokenSource _cancellation = new();

    void Establish()
    {
        _expected = new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>>();
        _inner.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 10, 20, _cancellation.Token).ReturnsForAnyArgs(_expected);
        _session.BeginReplaySession();
    }

    async Task Because() => _result = await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 10, 20, _cancellation.Token);

    [Fact] void should_forward_the_window_and_cancellation_token() => _inner.Received(1).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.SequenceEqual(new[] { _eventSourceId })),
        _eventTypeIds,
        10,
        20,
        _cancellation.Token);
    [Fact] void should_return_the_bounded_result() => ReferenceEquals(_result, _expected).ShouldBeTrue();

    void Destroy() => _cancellation.Dispose();
}
