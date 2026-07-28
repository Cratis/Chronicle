// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_Storage;

public class when_getting_an_event_store_for_the_first_time : given.a_storage
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    IEventStoreStorage _result;

    void Because() => _result = _storage.GetEventStore(_eventStore);

    [Fact] void should_return_a_storage_for_the_event_store() => _result.EventStore.ShouldEqual(_eventStore);

    [Fact] void should_register_the_event_store() => _collection.Received(1).ReplaceOneAsync(
        Arg.Any<FilterDefinition<EventStore>>(),
        Arg.Is<EventStore>(_ => _.Name == _eventStore),
        Arg.Is<ReplaceOptions>(_ => _.IsUpsert),
        Arg.Any<CancellationToken>());

    [Fact] void should_not_use_the_synchronous_driver_api() => _collection.DidNotReceive().ReplaceOne(
        Arg.Any<FilterDefinition<EventStore>>(),
        Arg.Any<EventStore>(),
        Arg.Any<ReplaceOptions>(),
        Arg.Any<CancellationToken>());
}
