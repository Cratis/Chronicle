// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_a_lookup_repeats_across_the_replay_boundary : given.a_replay_scoped_storage
{
    async Task Because()
    {
        _session.BeginReplaySession();
        await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);
        _session.EndReplaySession();
        await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);
    }

    [Fact] void should_query_again_after_replay_ends() =>
        _inner.Received(2).TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>());
}
