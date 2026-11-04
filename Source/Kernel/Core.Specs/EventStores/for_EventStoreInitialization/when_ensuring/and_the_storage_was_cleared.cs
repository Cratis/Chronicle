// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.when_ensuring;

public class and_the_storage_was_cleared : given.an_event_store_initialization
{
    IEventStoreStorage _replacement;

    async Task Establish()
    {
        await _initialization.Ensure(_name);
        _replacement = Substitute.For<IEventStoreStorage>();
        _replacement.IsInitialized().Returns(false);
        _storage.GetEventStore(_name).Returns(_replacement);
    }

    Task Because() => _initialization.Ensure(_name);

    [Fact] void should_read_the_new_stores_marker() => _replacement.Received().IsInitialized();
    [Fact] void should_call_the_initializer_again() => _initializerReference.Received(2).Ensure();
    [Fact] void should_initialize_the_new_store() => _replacement.Received(1).MarkInitialized();
}
