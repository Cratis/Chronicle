// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverHandledCountsStorage;

public class when_incrementing_in_bulk : Specification
{
    readonly ObserverHandledCountsStorage _storage = new();
    IReadOnlyDictionary<EventTypeId, EventCount> _first;
    IReadOnlyDictionary<EventTypeId, EventCount> _second;
    IReadOnlyDictionary<EventTypeId, EventCount> _empty;

    async Task Because()
    {
        await _storage.Increment("observer", "first", new Dictionary<EventTypeId, EventCount> { ["a"] = 1UL });
        await _storage.Increment("observer",
        [
            new((Key)"first", new Dictionary<EventTypeId, EventCount> { ["a"] = 2UL, ["b"] = 1UL }),
            new((Key)"second", new Dictionary<EventTypeId, EventCount> { ["a"] = 3UL }),
            new((Key)"empty", new Dictionary<EventTypeId, EventCount>())
        ]);
        await _storage.Increment("observer", []);
        _first = await _storage.GetFor("observer", "first");
        _second = await _storage.GetFor("observer", "second");
        _empty = await _storage.GetFor("observer", "empty");
    }

    [Fact] void should_accumulate_existing_counts() => _first["a"].ShouldEqual((EventCount)3UL);
    [Fact] void should_increment_other_types() => _first["b"].ShouldEqual((EventCount)1UL);
    [Fact] void should_increment_other_partitions() => _second["a"].ShouldEqual((EventCount)3UL);
    [Fact] void should_skip_empty_counts() => _empty.ShouldBeEmpty();
}
