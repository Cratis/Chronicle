// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.when_setting_the_binding_epoch;

public abstract class and_it_is_repeated<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    bool _result;
    async Task Because() { _result = await _storage.TrySetBindingEpoch(_store, _key, ServingEpoch.Unset, ServingEpoch.Initial); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.ShouldBeTrue();
        await Task.CompletedTask;
    }
}
