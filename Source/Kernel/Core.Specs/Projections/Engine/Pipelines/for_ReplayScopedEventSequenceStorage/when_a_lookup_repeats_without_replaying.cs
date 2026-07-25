// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_a_lookup_repeats_without_replaying : given.a_replay_scoped_storage
{
    async Task Because()
    {
        await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);
        await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);
    }

    [Fact] void should_pass_every_lookup_through_to_storage() =>
        _inner.Received(2).TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>());
}
