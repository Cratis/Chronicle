// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_adding_generations;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_provenance_is_recorded(ReplicaSetMongoDBFixture fixture) : given.a_storage_with_stored_generations(fixture)
{
    GenerationProvenance _provenance;

    async Task Because()
    {
        (await _storage.TryAddGenerations(_observed, [Target()])).ShouldBeTrue();
        _provenance = await Provenance(3);
    }

    [Fact] void should_record_the_source() => _provenance.Source.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_record_the_appended_flag() => _provenance.SourceIsAppended.ShouldBeTrue();
    [Fact] void should_record_the_version() => _provenance.MigrationsVersion.ShouldEqual(Version);
}
