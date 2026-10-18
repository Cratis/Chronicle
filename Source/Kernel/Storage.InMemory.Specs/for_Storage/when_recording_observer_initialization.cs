// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.for_Storage;

public class when_recording_observer_initialization : given.a_storage
{
    bool _before;
    bool _after;
    bool _other;
    bool _setup;

    async Task Establish()
    {
        await _storage.GetEventStore("orders").MarkInitialized();
        _before = await _storage.GetEventStore("orders").AreObserversInitialized();
    }

    async Task Because()
    {
        await _storage.GetEventStore("orders").MarkObserversInitialized();
        await _storage.GetEventStore("orders").MarkObserversInitialized();
        var reconstructed = new Storage(_eventStoreStorages, Substitute.For<ISystemStorage>());
        _after = await reconstructed.GetEventStore("orders").AreObserversInitialized();
        _other = await reconstructed.GetEventStore("other").AreObserversInitialized();
        _setup = await reconstructed.GetEventStore("orders").IsInitialized();
    }

    [Fact] void should_not_treat_store_setup_as_observer_initialization() => _before.ShouldBeFalse();
    [Fact] void should_keep_observer_initialization_across_storage_instances() => _after.ShouldBeTrue();
    [Fact] void should_not_initialize_another_store() => _other.ShouldBeFalse();
    [Fact] void should_keep_store_setup() => _setup.ShouldBeTrue();
}
