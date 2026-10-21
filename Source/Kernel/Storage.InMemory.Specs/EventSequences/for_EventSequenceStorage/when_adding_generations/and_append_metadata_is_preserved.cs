// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_append_metadata_is_preserved : given.a_storage_with_stored_generations
{
    StoredEventMetadata _before;
    StoredEventMetadata _after;
    AppendedEvent _read;

    async Task Establish() => _before = (await _storage.GetMetadataAt([0UL])).Single();

    async Task Because()
    {
        (await _storage.TryAddGenerations(_observed, [Target()])).ShouldBeTrue();
        _after = (await _storage.GetMetadataAt([0UL])).Single();
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_leave_append_metadata_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_keep_the_appended_generation() => _storage.GetAppendedGeneration(0).ShouldEqual((uint?)1);
    [Fact] void should_deliver_the_added_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(3));
}
