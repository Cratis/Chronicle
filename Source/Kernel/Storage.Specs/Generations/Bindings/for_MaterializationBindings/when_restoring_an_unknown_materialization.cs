// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_restoring_an_unknown_materialization<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _unknown;
    MaterializationPublication _known;

    async Task Because()
    {
        _unknown = await _storage.TryRestore(_store, _key with { Projection = "unknown" }, MaterializationGeneration.First, ServingEpoch.Initial, new(99), _rollbackWindow, _receipt);
        _known = await _storage.TryRestore(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, new(99), _rollbackWindow, _receipt);
    }

    [Fact] public void should_report_the_unknown_materialization() => _unknown.Result.ShouldEqual(MaterializationPublicationResult.UnknownMaterialization);
    [Fact] public void should_not_record_the_unknown_outcome() => _known.Result.ShouldEqual(MaterializationPublicationResult.CandidateNotRestorable);
}
