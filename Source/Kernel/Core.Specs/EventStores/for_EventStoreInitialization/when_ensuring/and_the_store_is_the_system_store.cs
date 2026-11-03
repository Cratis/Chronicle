// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.when_ensuring;

public class and_the_store_is_the_system_store : given.an_event_store_initialization
{
    async Task Because()
    {
        await _initialization.Ensure(EventStoreName.System);
        await _initialization.Ensure("system");
    }

    [Fact] void should_not_access_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_call_grains() => _grains.ReceivedCalls().ShouldBeEmpty();
}
