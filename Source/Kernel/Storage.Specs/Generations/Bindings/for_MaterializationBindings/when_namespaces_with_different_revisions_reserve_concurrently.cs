// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_namespaces_with_different_revisions_reserve_concurrently<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationKey _firstKey;
    MaterializationKey _secondKey;
    MaterializationExecutionSpecification _firstExecution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"one\",\"schema\":{\"type\":\"object\"}}");
    MaterializationExecutionSpecification _secondExecution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"projection\":\"two\",\"schema\":{\"type\":\"object\"}}");
    GenerationReservation[] _results;

    async Task Establish()
    {
        _firstKey = await Bind("first", _firstExecution);
        _secondKey = await Bind("second", _secondExecution);
    }

    async Task Because() => _results = await Task.WhenAll(
        Task.Run(() => _builds.ReserveNext(_store, _firstKey, _firstExecution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch)),
        Task.Run(() => _builds.ReserveNext(_store, _secondKey, _secondExecution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch)));

    [Fact] public void should_reserve_both() => _results.All(_ => _.Result == GenerationReservationResult.ReservationCreated).ShouldBeTrue();
    [Fact] public void should_reserve_distinct_numbers() => _results.Select(_ => _.Record!.Identity.Generation).Distinct().Count().ShouldEqual(2);
    [Fact] public async Task should_bind_each_number_to_its_own_revision()
    {
        (await _harness.Claims.Get(_store, _results[0].Record!.Identity.ObserverId))!.Revision.ShouldEqual(_firstExecution.Revision);
        (await _harness.Claims.Get(_store, _results[1].Record!.Identity.ObserverId))!.Revision.ShouldEqual(_secondExecution.Revision);
    }

    async Task<MaterializationKey> Bind(string ns, MaterializationExecutionSpecification execution)
    {
        var key = _key with { Namespace = ns };
        await _storage.TryAdoptFirst(_store, key, _first);
        await _storage.TrySetBindingEpoch(_store, key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(_store, key, execution);
        return key;
    }
}
