// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_discarding_prepared_parent_history : given.a_bounded_parent_page
{
    async Task Because()
    {
        await Prepare();
        ClearPreparedPage();
        await ReadChild();
        await Prepare();
        _session.EndReplaySession();
        _session.BeginReplaySession();
        await ReadChild();
        await Prepare();
        BeginPreparedPage();
        await ReadChild();
    }

    [Fact] void should_fetch_again_after_page_clear_session_end_and_page_replacement() => _inner.Received(3).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.SequenceEqual(new[] { _eventSourceId })), _eventTypeIds, 13, 12, Arg.Any<CancellationToken>());
    [Fact] void should_prefetch_each_new_session() => _inner.Received(3).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.Count() == 2), _eventTypeIds, 10, 20, Arg.Any<CancellationToken>());

    Task ReadChild() => _storage.GetInstancesOfAnyInRange([_eventSourceId], _eventTypeIds, 13, 12);
}
