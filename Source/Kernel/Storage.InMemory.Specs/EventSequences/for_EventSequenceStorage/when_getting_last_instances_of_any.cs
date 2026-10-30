// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_getting_last_instances_of_any : given.a_storage_with_appended_events
{
    IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>> _results;
    readonly EventSourceId _missing = "missing";
    bool _matchesSingles;

    async Task Because()
    {
        _results = await _storage.GetLastInstancesOfAny([_firstEventSourceId, _secondEventSourceId, _missing, _firstEventSourceId], [_eventType.Id]);
        _matchesSingles = true;
        foreach (var (source, result) in _results)
        {
            _matchesSingles &= result.Equals(await _storage.TryGetLastInstanceOfAny(source, [_eventType.Id]));
        }
    }

    [Fact] void should_equal_the_single_source_reads() => _matchesSingles.ShouldBeTrue();
    [Fact] void should_include_each_source_once() => _results.Count.ShouldEqual(3);
    [Fact] void should_include_missing_sources() => _results[_missing].TryGetValue(out _).ShouldBeFalse();
    [Fact]
    void should_take_the_later_event_in_the_whole_log()
    {
        _results[_firstEventSourceId].TryGetValue(out var last).ShouldBeTrue();
        last.Context.SequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
    }
}
