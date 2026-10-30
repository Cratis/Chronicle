// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationAdmission;

public class when_resetting_before_admission : given.a_provider
{
    Exception _error;

    async Task Establish()
    {
        await Advance(2);
        _storage.Clear();
    }

    async Task Because() => _error = await Catch.Exception(() => _admission.Admit(CancellationToken.None));

    [Fact] void should_refuse_the_below_floor_binary() => _error.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] async Task should_not_reopen_a_store() => (await _storage.GetEventStores()).ShouldBeEmpty();
    [Fact] async Task should_preserve_the_floor() => (await _storage.ReadGenerationFloors())[_name].Revision.ShouldEqual(1UL);
}
