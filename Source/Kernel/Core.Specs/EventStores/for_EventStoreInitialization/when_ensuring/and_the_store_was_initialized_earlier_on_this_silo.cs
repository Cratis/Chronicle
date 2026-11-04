// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.when_ensuring;

public class and_the_store_was_initialized_earlier_on_this_silo : given.an_event_store_initialization
{
    async Task Establish()
    {
        await _initialization.Ensure(_name);
        _store.ClearReceivedCalls();
        _grains.ClearReceivedCalls();
        _storage.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _initialization.Ensure(_name);
        await _initialization.Ensure(_name);
        await _initialization.Ensure(_name);
    }

    [Fact] void should_not_read_storage() => _store.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_call_grains() => _grains.ReceivedCalls().ShouldBeEmpty();
}
