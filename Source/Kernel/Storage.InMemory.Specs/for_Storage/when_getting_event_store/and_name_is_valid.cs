// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_Storage.when_getting_event_store;

public class and_name_is_valid : given.a_storage
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    IEventStoreStorage _result;

    void Because() => _result = _storage.GetEventStore(_eventStore);

    [Fact] void should_return_a_storage() => _result.ShouldNotBeNull();
    [Fact] void should_return_a_storage_for_the_event_store() => _result.EventStore.ShouldEqual(_eventStore);
}
