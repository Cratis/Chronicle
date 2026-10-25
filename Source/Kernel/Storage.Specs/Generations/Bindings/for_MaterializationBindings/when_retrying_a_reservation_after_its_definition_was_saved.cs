// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_retrying_a_reservation_after_its_definition_was_saved<THarness> : given.a_build_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    GenerationReservation _original;
    GenerationReservation _retry;
    Exception _error;
    async Task Establish()
    {
        _original = await Build();
        await _harness.SaveObserverDefinition(_store, _original.Record!.Identity.ObserverId);
    }
    async Task Because() => _error = await Catch.Exception(async () => _retry = await Build());
    [Fact] public void should_not_conflict() => _error.ShouldBeNull();
    [Fact] public void should_return_the_same_reservation() => _retry.ShouldEqual(_original);
}
