// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

public abstract class a_build_storage<THarness> : a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    protected IGenerationBuildStorage _builds;
    protected MaterializationExecutionSpecification _execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"snapshot\",\"schema\":{\"type\":\"object\"}}");
    protected GenerationBuildOperationId _operation = new(Guid.NewGuid());

    async Task Establish()
    {
        _builds = (IGenerationBuildStorage)_storage;
        await _builds.TryAdoptExecution(_store, _key, _execution);
    }

    protected Task<GenerationReservation> Build(GenerationBuildOperationId? operation = null) => _builds.ReserveNext(_store, _key, _execution, operation ?? _operation, DateTimeOffset.UnixEpoch);
}
