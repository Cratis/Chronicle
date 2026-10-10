// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_the_appended_generation_is_lower : given.a_storage_with_stored_generations
{
    bool _added;
    StoredEventGenerations _after;

    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [Target()]);
        _after = (await _storage.GetStoredGenerations(0))!;
    }

    [Fact] void should_add_the_missing_generation() => _added.ShouldBeTrue();
    [Fact] void should_preserve_the_original() => _after.Content[new EventTypeGeneration(1)].ShouldEqual(_observed.Content[new EventTypeGeneration(1)]);
    [Fact] void should_add_the_target_content() => _after.Content.ContainsKey(new EventTypeGeneration(3)).ShouldBeTrue();
    [Fact] async Task should_write_the_hash() => (await Hash(3)).ShouldEqual("added-hash");
}
