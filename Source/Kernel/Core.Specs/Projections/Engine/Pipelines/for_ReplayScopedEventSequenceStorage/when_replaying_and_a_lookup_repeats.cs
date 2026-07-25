// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_replaying_and_a_lookup_repeats : given.a_replay_scoped_storage
{
    async Task Because()
    {
        _session.BeginReplaySession();

        await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);
        await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);

        await _storage.GetHeadSequenceNumber(_eventTypes, _eventSourceId);
        await _storage.GetHeadSequenceNumber(_eventTypes, _eventSourceId);
    }

    [Fact] void should_query_the_last_instance_once() =>
        _inner.Received(1).TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>());

    [Fact] void should_query_the_head_sequence_number_once() =>
        _inner.Received(1).GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>());
}
