// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

/// <summary>
/// Documents the accepted gap: a reservation losing to another revision in the same namespace may leave its claimed number
/// unreferenced by any binding. Which operation wins, and whether a number is left over, is not deterministic, so only the
/// outcome that always holds is asserted.
/// </summary>
/// <typeparam name="THarness">The type of the storage harness.</typeparam>
public abstract class when_one_namespace_reserves_different_revisions_concurrently<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationExecutionSpecification _otherExecution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"other\",\"schema\":{\"type\":\"object\"}}");
    GenerationReservation[] _results;
    GenerationReservation _winner;
    GenerationReservation _later;
    MaterializationExecutionSpecification _loserExecution;

    async Task Because()
    {
        _results = await Task.WhenAll(
            Task.Run(() => _builds.ReserveNext(_store, _key, _execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch)),
            Task.Run(() => _builds.ReserveNext(_store, _key, _otherExecution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch)));
        var created = _results.Single(_ => _.Result == GenerationReservationResult.ReservationCreated);
        _winner = created;
        _loserExecution = created == _results[0] ? _otherExecution : _execution;
        await _storage.TryAbandon(_store, _key, created.Record!.Identity.Generation);
        _later = await _builds.ReserveNext(_store, _key, _loserExecution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
    }

    [Fact] public void should_create_one_reservation() => _results.Count(_ => _.Result == GenerationReservationResult.ReservationCreated).ShouldEqual(1);
    [Fact] public void should_refuse_the_other_with_a_candidate_in_flight() => _results.Count(_ => _.Result == GenerationReservationResult.CandidateInFlight).ShouldEqual(1);
    [Fact] public void should_reserve_the_loser_revision_after_the_candidate_is_abandoned() => _later.Result.ShouldEqual(GenerationReservationResult.ReservationCreated);
    [Fact] public void should_not_reuse_the_winning_number() => _later.Record!.Identity.Generation.ShouldNotEqual(_winner.Record!.Identity.Generation);
    [Fact] public async Task should_bind_the_later_number_to_the_loser_revision() => (await _harness.Claims.Get(_store, _later.Record!.Identity.ObserverId))!.Revision.ShouldEqual(_loserExecution.Revision);
}
