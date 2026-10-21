// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_the_event_is_revised_keeps_revision_visible : given.a_storage_with_stored_generations
{
    AppendedEvent _read;

    async Task Establish()
    {
        await Revise();
        _observed = (await _storage.GetStoredGenerations(0))!;
    }

    async Task Because()
    {
        (await _storage.TryAddGenerations(_observed, [Target()])).ShouldBeTrue();
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_keep_the_revision_generation() => _read.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_keep_the_revision_content() => _read.Content.ShouldBeEmpty();
    [Fact] void should_still_add_the_base_generation() => _read.GenerationalContent.ContainsKey(3).ShouldBeTrue();
}
