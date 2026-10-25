// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reading_a_revoked_grant<THarness> : given.a_reserved_generation_binding<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _publication;
    GenerationWriterGrant? _result;

    async Task Establish()
    {
        await Acquire(0);
        await _storage.TryMarkFollowing(_store, _key, _candidate);
        _publication = await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow, _receipt);
    }

    async Task Because() => _result = await _grants.GetAdmitted(_store, _binding);

    [Fact] public void should_have_revoked_through_publication() => _publication.Result.ShouldEqual(MaterializationPublicationResult.Published);
    [Fact] public void should_return_nothing() => _result.ShouldBeNull();
}
