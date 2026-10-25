// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_namespaces_with_the_same_revision_reserve_concurrently<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationKey _firstKey;
    MaterializationKey _secondKey;
    GenerationReservation[] _results;

    async Task Establish()
    {
        _firstKey = await Bind("first", _execution);
        _secondKey = await Bind("second", _execution);
    }

    async Task Because() => _results = await Task.WhenAll(
        Task.Run(() => _builds.ReserveNext(_store, _firstKey, _execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch)),
        Task.Run(() => _builds.ReserveNext(_store, _secondKey, _execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch)));

    [Fact] public void should_reserve_both() => _results.All(_ => _.Result == GenerationReservationResult.ReservationCreated).ShouldBeTrue();
    [Fact] public void should_share_one_number() => _results.Select(_ => _.Record!.Identity.Generation).Distinct().Count().ShouldEqual(1);

    async Task<MaterializationKey> Bind(string ns, MaterializationExecutionSpecification execution)
    {
        var key = _key with { Namespace = ns };
        await _storage.TryAdoptFirst(_store, key, _first);
        await _storage.TrySetBindingEpoch(_store, key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(_store, key, execution);
        return key;
    }
}
