// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reserving_the_next_generation<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation _result;
    async Task Because() { _result = await _storage.ReserveNext(_store, _key, "revision", DateTimeOffset.UnixEpoch); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.Result.ShouldEqual(GenerationReservationResult.ReservationCreated);
        _result.Record!.Identity.Generation.ShouldEqual(new MaterializationGeneration(2));
        _result.Record.Identity.Destination.ShouldEqual(new ReadModelContainerName("models-g2"));
        _result.Record.Identity.ObserverId.Value.ShouldEqual("projection@g2");
        _result.Record.State.ShouldEqual(MaterializationGenerationState.Building);
        await Task.CompletedTask;
    }
}
