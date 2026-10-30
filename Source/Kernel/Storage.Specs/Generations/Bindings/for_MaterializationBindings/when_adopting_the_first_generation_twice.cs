// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_adopting_the_first_generation_twice<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    FirstGenerationAdoption _result;
    async Task Because() { _result = await _storage.TryAdoptFirst(_store, _key, _first); }
    [Fact] public async Task should_observe_the_contract()
    {
        _result.ShouldEqual(FirstGenerationAdoption.AlreadyAdopted);
        await Task.CompletedTask;
    }
}
