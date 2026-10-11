// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_reading;

public class and_a_higher_generation_was_added_later : given.a_storage_for_appended_generation
{
    AppendedEvent _read;

    async Task Establish()
    {
        var content = Content();
        var entry = EventAt(EventSequenceNumber.First, 1) with
        {
            GenerationalContent = new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = content[EventTypeGeneration.First] }
        };
        (await _storage.AppendMany([entry])).IsSuccess.ShouldBeTrue();
        var observed = (await _storage.GetStoredGenerations(EventSequenceNumber.First))!;
        var target = new GenerationToAdd(new EventTypeGeneration(2), content[new EventTypeGeneration(2)], "migrated-hash", new(EventTypeGeneration.First, true, new("migration-version")));
        (await _storage.TryAddGenerations(observed, [target])).ShouldBeTrue();
    }

    async Task Because() => _read = await _storage.GetEventAt(EventSequenceNumber.First);

    [Fact] void should_read_the_highest_stored_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
    [Fact] void should_preserve_the_appended_generation() => _read.Context.AppendedGeneration.ShouldEqual(EventTypeGeneration.First);
}
