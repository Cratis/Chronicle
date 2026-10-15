// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_appended_generation_is_unknown : given.a_storage_with_stored_generations
{
    GenerationProvenance _provenance;
    StoredEventGenerations _after;

    async Task Establish()
    {
        await ClearAppendedGeneration();
        _observed = (await _storage.GetStoredGenerations(0))!;
    }

    async Task Because()
    {
        (await _storage.TryAddGenerations(_observed, [Target(source: 2, sourceIsAppended: false)])).ShouldBeTrue();
        _provenance = await Provenance(3);
        _after = (await _storage.GetStoredGenerations(0))!;
    }

    [Fact] void should_leave_the_appended_generation_unknown() => _after.AppendedGeneration.ShouldBeNull();
    [Fact] void should_record_the_derived_source() => _provenance.Source.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_not_claim_an_original_source() => _provenance.SourceIsAppended.ShouldBeFalse();
}
