// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_a_prefetched_lookup_crosses_the_replay_boundary : given.a_replay_scoped_storage
{
    void Establish() => _inner.GetLastInstancesOfAny(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>())
        .Returns(Task.FromResult<IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>>>(new Dictionary<EventSourceId, Option<AppendedEvent>> { [_eventSourceId] = Option<AppendedEvent>.None() }));

    async Task Because()
    {
        _session.BeginReplaySession();
        await _storage.GetLastInstancesOfAny([_eventSourceId], _eventTypeIds);
        _session.EndReplaySession();
        _session.BeginReplaySession();
        await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);
    }

    [Fact] void should_issue_its_own_lookup_in_the_new_session() => _inner.Received(1).TryGetLastInstanceOfAny(_eventSourceId, Arg.Any<IEnumerable<EventTypeId>>());
}
