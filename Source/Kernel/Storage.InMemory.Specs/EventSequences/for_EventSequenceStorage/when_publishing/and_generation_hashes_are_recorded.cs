// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_publishing;

public class and_generation_hashes_are_recorded : given.a_storage_for_appended_generation
{
    async Task Because()
    {
        var entry = EventAt(0, 1) with
        {
            ContentHashes = new Dictionary<EventTypeGeneration, EventHash> { [1] = "original-hash", [2] = "migrated-hash" }
        };
        (await _storage.AppendPublication(new EventPublication("publication", "intent"), entry)).IsSuccess.ShouldBeTrue();
    }

    [Fact] void should_record_the_original_hash() => _storage.GetGenerationHashes(0)[EventTypeGeneration.First].ShouldEqual(new EventHash("original-hash"));
    [Fact] void should_record_the_migrated_hash() => _storage.GetGenerationHashes(0)[new EventTypeGeneration(2)].ShouldEqual(new EventHash("migrated-hash"));
}
