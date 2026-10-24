// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_prefetching_during_replay : given.a_replay_scoped_storage
{
    readonly EventSourceId _missing = "missing";
    AppendedEvent _last;
    Option<AppendedEvent> _hit;
    Option<AppendedEvent> _miss;

    void Establish()
    {
        _last = AppendedEvent.EmptyWithEventSequenceNumber(100UL);
        _inner.GetLastInstancesOfAny(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Task.FromResult<IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>>>(new Dictionary<EventSourceId, Option<AppendedEvent>>
            {
                [_eventSourceId] = _last,
                [_missing] = Option<AppendedEvent>.None()
            }));
        _session.BeginReplaySession();
    }

    async Task Because()
    {
        await _storage.GetLastInstancesOfAny([_eventSourceId, _missing], _eventTypeIds);
        _hit = await _storage.TryGetLastInstanceOfAny(_eventSourceId, _eventTypeIds);
        _miss = await _storage.TryGetLastInstanceOfAny(_missing, _eventTypeIds);
    }

    [Fact]
    void should_seed_the_latest_event()
    {
        _hit.TryGetValue(out var last).ShouldBeTrue();
        last.ShouldBeSame(_last);
    }
    [Fact] void should_seed_the_missing_source() => _miss.TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_not_issue_individual_reads() => _inner.DidNotReceive().TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>());
    [Fact] void should_query_the_batch_once() => _inner.Received(1).GetLastInstancesOfAny(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>());
}
