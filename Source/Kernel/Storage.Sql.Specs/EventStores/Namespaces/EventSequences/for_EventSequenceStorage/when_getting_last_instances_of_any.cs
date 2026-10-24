// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_getting_last_instances_of_any : given.a_storage_for_appended_generation
{
    IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>> _results;
    readonly EventSourceId _source = "some-source";
    readonly EventSourceId _missing = "missing";
    bool _matchesSingles;

    async Task Establish() => await _storage.AppendMany([EventAt(0, 1), EventAt(1, 1) with { EventSourceId = "other-source" }, EventAt(20, 1)]);

    async Task Because()
    {
        _results = await _storage.GetLastInstancesOfAny([_source, new("other-source"), _missing, _source], [_eventType.Id]);
        _matchesSingles = true;
        foreach (var (source, result) in _results)
        {
            var single = await _storage.TryGetLastInstanceOfAny(source, [_eventType.Id]);
            _matchesSingles &= result.TryGetValue(out var batched) == single.TryGetValue(out var individual) &&
                (batched is null || batched.Context.SequenceNumber == individual.Context.SequenceNumber);
        }
    }

    [Fact] void should_equal_the_single_source_reads() => _matchesSingles.ShouldBeTrue();
    [Fact] void should_include_each_source_once() => _results.Count.ShouldEqual(3);
    [Fact] void should_include_missing_sources() => _results[_missing].TryGetValue(out _).ShouldBeFalse();
    [Fact]
    void should_take_the_later_event_in_the_whole_log()
    {
        _results[_source].TryGetValue(out var last).ShouldBeTrue();
        last.Context.SequenceNumber.ShouldEqual((EventSequenceNumber)20UL);
    }
}
