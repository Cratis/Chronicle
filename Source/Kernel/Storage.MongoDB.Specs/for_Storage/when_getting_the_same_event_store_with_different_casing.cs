// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_Storage;

public class when_getting_the_same_event_store_with_different_casing : given.a_storage
{
    static readonly EventStoreName _eventStore = "MyStore";
    static readonly EventStoreName _differentlyCasedEventStore = "mystore";
    IEventStoreStorage _first;
    IEventStoreStorage _second;

    void Establish() => _first = _storage.GetEventStore(_eventStore);

    void Because() => _second = _storage.GetEventStore(_differentlyCasedEventStore);

    [Fact] void should_return_the_same_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_only_create_storage_once() => _database.EventStoreDatabaseCalls.ShouldEqual(1);
}
