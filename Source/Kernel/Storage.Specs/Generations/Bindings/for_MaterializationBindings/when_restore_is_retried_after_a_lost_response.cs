// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_restore_is_retried_after_a_lost_response<THarness> : given.a_published_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _original;
    MaterializationPublication _retried;
    MaterializationBinding _restored;

    async Task Establish()
    {
        _original = await Restore();
        _restored = (await _storage.Get(_store, _key))!;
        _time.Advance(_rollbackWindow + TimeSpan.FromHours(1));
    }

    async Task Because() => _retried = await Restore();

    [Fact] public void should_return_the_recorded_outcome() => _retried.ShouldEqual(_original);
    [Fact] public async Task should_not_repeat_the_publication()
    {
        var binding = (await _storage.Get(_store, _key))!;
        binding.Epoch.ShouldEqual(_before.Epoch.Next());
        binding.Fence.ShouldEqual(_restored.Fence);
        binding.Generations.SequenceEqual(_restored.Generations).ShouldBeTrue();
    }
}
