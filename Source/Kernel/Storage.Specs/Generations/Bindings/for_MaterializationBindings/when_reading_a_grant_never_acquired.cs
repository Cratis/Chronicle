// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reading_a_grant_never_acquired<THarness> : given.a_reserved_generation_binding<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationWriterGrant? _result;

    async Task Because() => _result = await _grants.GetAdmitted(_store, _binding);

    [Fact] public void should_return_nothing() => _result.ShouldBeNull();
    [Fact] public async Task should_still_admit_the_owner_at_the_initial_epoch() => (await Acquire(0)).ShouldNotBeNull();
}
