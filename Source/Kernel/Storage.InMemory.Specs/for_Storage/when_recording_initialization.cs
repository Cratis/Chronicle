// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.for_Storage;

public class when_recording_initialization : given.a_storage
{
    bool _wasInitialized;
    bool _initializedFromAnotherStorage;
    bool _otherStoreInitialized;

    async Task Establish() => _wasInitialized = await _storage.GetEventStore("orders").IsInitialized();

    async Task Because()
    {
        await _storage.GetEventStore("orders").MarkInitialized();
        await _storage.GetEventStore("orders").MarkInitialized();
        var anotherStorage = new Storage(_eventStoreStorages, Substitute.For<ISystemStorage>());
        _initializedFromAnotherStorage = await anotherStorage.GetEventStore("orders").IsInitialized();
        _otherStoreInitialized = await anotherStorage.GetEventStore("other").IsInitialized();
    }

    [Fact] void should_not_confuse_implicit_creation_with_initialization() => _wasInitialized.ShouldBeFalse();
    [Fact] void should_share_completion_with_other_storage_instances() => _initializedFromAnotherStorage.ShouldBeTrue();
    [Fact] void should_not_mark_other_stores_as_initialized() => _otherStoreInitialized.ShouldBeFalse();
}
