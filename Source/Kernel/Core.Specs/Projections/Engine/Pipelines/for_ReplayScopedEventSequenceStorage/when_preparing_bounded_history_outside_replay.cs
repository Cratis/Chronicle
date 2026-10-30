// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_preparing_bounded_history_outside_replay : given.a_bounded_parent_page
{
    async Task Because()
    {
        BeginPreparedPage();
        await _storage.GetInstancesOfAnyInRange([_eventSourceId, _missing], _eventTypeIds, 10, 20);
        await _storage.GetInstancesOfAnyInRange([_eventSourceId, _missing], _eventTypeIds, 10, 20);
        _session.BeginReplaySession();
        await _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 13, 12);
    }

    [Fact] void should_forward_every_read_without_a_replay_session() => _inner.Received(2).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.Count() == 2), _eventTypeIds, 10, 20, Arg.Any<CancellationToken>());
    [Fact] void should_not_seed_the_next_session() => _inner.Received(1).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.Count() == 1), _eventTypeIds, 13, 12, Arg.Any<CancellationToken>());
}
