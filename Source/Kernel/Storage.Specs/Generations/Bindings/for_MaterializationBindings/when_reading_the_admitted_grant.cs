// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reading_the_admitted_grant<THarness> : given.a_reserved_generation_binding<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationWriterGrant _acquired;
    GenerationWriterGrant? _result;
    GenerationWriterGrant? _again;

    async Task Establish() => _acquired = (await Acquire(0))!;

    async Task Because()
    {
        _result = await _grants.GetAdmitted(_store, _binding);
        _again = await _grants.GetAdmitted(_store, _binding);
    }

    [Fact] public void should_return_the_stored_grant() => _result.ShouldEqual(_acquired);
    [Fact] public void should_not_advance_the_epoch_when_read() => _again.ShouldEqual(_acquired);
}
