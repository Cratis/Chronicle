// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_build_operations_compete<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation[] _results;
    async Task Because() => _results = await Task.WhenAll(Build(), Build(new(Guid.NewGuid())));
    [Fact] public void should_reserve_one_candidate() => _results.Count(_ => _.Result == GenerationReservationResult.ReservationCreated).ShouldEqual(1);
    [Fact] public void should_refuse_the_competing_operation() => _results.Count(_ => _.Result == GenerationReservationResult.CandidateInFlight).ShouldEqual(1);
    [Fact] public void should_identify_the_same_candidate() => _results.Select(_ => _.Record!.Identity).Distinct().Count().ShouldEqual(1);
}
