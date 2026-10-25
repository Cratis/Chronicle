// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_reading_a_superseded_grant<THarness> : given.a_reserved_generation_binding<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationWriterGrant _old;
    GenerationWriterGrant _new;
    GenerationWriterGrant? _result;

    async Task Establish()
    {
        _old = (await Acquire(0))!;
        _new = (await Acquire(_old.Epoch))!;
    }

    async Task Because() => _result = await _grants.GetAdmitted(_store, _binding);

    [Fact] public void should_return_the_new_grant() => _result.ShouldEqual(_new);
    [Fact] public void should_not_return_the_old_attempt() => _result!.Attempt.ShouldNotEqual(_old.Attempt);
    [Fact] public void should_not_return_the_old_epoch() => _result!.Epoch.ShouldNotEqual(_old.Epoch);
}
