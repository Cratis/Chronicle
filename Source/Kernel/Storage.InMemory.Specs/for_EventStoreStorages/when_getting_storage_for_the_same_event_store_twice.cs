// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

public class when_getting_storage_for_the_same_event_store_twice : given.an_empty_registry
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    IEventStoreStorage _first;
    IEventStoreStorage _second;

    void Establish() => _first = _storages.GetOrCreate(_eventStore);

    void Because() => _second = _storages.GetOrCreate(_eventStore);

    [Fact] void should_return_the_same_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();
}
