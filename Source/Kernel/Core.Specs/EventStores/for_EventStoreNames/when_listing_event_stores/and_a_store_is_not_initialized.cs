// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_listing_event_stores;

public class and_a_store_is_not_initialized : Specification
{
    IStorage _storage;
    IEventStoreInitialization _initialization;
    IEnumerable<EventStoreNames> _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStores().Returns([(EventStoreName)"ready", "not-ready"]);
        _initialization = Substitute.For<IEventStoreInitialization>();
        _initialization.EnsureAll(Arg.Is<IEnumerable<EventStoreName>>(stores => stores.SequenceEqual(new EventStoreName[] { "ready", "not-ready" })))
            .Returns([(EventStoreName)"ready"]);
    }

    async Task Because() => _result = await EventStoreNames.AllEventStores(_storage, _initialization);

    [Fact] void should_only_list_initialized_stores() => _result.ShouldEqual([new EventStoreNames("ready", "ready")]);
}
