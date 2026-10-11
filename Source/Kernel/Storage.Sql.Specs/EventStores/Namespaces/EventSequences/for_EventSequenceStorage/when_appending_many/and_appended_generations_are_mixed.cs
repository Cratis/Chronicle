// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_appended_generations_are_mixed : given.a_storage_for_appended_generation
{
    EventEntry _first;
    EventEntry _second;
    AppendedEvent _read;
    AppendedEvent[] _acknowledged;

    async Task Because()
    {
        var first = EventAt(0, 1) with
        {
            ContentHashes = new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "original-hash", [new EventTypeGeneration(2)] = "migrated-hash" }
        };
        var second = EventAt(1, 2) with { ContentHashes = first.ContentHashes };
        var result = await _storage.AppendMany([first, second]);
        result.IsSuccess.ShouldBeTrue();
        _acknowledged = result.AsT0.ToArray();
        _first = Stored(0);
        _second = Stored(1);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_expose_the_original_generation_on_reads() => _read.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_expose_every_generations_hash() => _read.GenerationalHashes.Keys.ShouldContainOnly(1, 2);
    [Fact] void should_preserve_the_original_generations_hash() => _read.GenerationalHashes[1].Value.ShouldEqual("original-hash");
    [Fact] void should_preserve_the_migrated_generations_hash() => _read.GenerationalHashes[2].Value.ShouldEqual("migrated-hash");
    [Fact] void should_expose_generation_hashes_live() => _acknowledged[0].GenerationalHashes[2].Value.ShouldEqual("migrated-hash");
    [Fact] void should_expose_the_original_appended_generation_live() => _acknowledged[0].Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_expose_the_later_appended_generation_live() => _acknowledged[1].Context.AppendedGeneration!.Value.ShouldEqual(2U);
    [Fact] void should_record_the_first_events_appended_generation() => _first.AppendedGeneration.ShouldEqual((uint?)1);
    [Fact] void should_record_the_second_events_appended_generation() => _second.AppendedGeneration.ShouldEqual((uint?)2);
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
