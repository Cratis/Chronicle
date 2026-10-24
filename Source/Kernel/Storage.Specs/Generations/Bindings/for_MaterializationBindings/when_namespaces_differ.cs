// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_namespaces_differ<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationKey _other;
    async Task Establish()
    {
        _other = _key with { Namespace = new("two") };
        await _storage.TryAdoptFirst(_store, _other, _first);
        await _storage.TrySetBindingEpoch(_store, _other, ServingEpoch.Unset, ServingEpoch.Initial);
        await Reserve(true);
    }
    async Task Because() { await _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, _rollbackWindow, _receipt); }
    [Fact] public async Task should_observe_the_contract()
    {
        (await _storage.Get(_store, _other))!.Active.ShouldEqual(MaterializationGeneration.First);
        (await _storage.Get(_store, _other))!.Epoch.ShouldEqual(ServingEpoch.Initial);
        await Task.CompletedTask;
    }
}
