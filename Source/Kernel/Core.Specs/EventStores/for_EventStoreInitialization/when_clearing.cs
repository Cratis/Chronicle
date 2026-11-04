// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization;

public class when_clearing : given.an_event_store_initialization
{
    async Task Establish()
    {
        await _initialization.Ensure(_name);
        _store.ClearReceivedCalls();
    }

    async Task Because()
    {
        _initialization.Clear();
        await _initialization.Ensure(_name);
    }

    [Fact] void should_read_the_marker_again() => _store.Received(1).IsInitialized();
}
