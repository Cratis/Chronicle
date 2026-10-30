// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationAdmission;

public class when_admitting_a_fresh_provider : given.a_provider
{
    async Task Because() => await _admission.Admit(CancellationToken.None);

    [Fact] async Task should_not_register_a_store() => (await _storage.GetEventStores()).ShouldBeEmpty();
    [Fact] async Task should_not_establish_a_floor() => (await _storage.ReadGenerationFloors()).ShouldBeEmpty();
}
