// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reserving_with_a_candidate_in_flight<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation _result;
    async Task Establish()
    {
        await Reserve();
    }
    async Task Because() { _result = await _storage.ReserveNext(_store, _key, "other", DateTimeOffset.UnixEpoch); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.Result.ShouldEqual(GenerationReservationResult.CandidateInFlight);
        _result.Record!.Identity.Generation.ShouldEqual(_candidate);
        await Task.CompletedTask;
    }
}
