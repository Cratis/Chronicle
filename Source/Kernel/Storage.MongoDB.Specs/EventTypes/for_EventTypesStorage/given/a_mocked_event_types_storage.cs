// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using EventTypesStorage = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventTypesStorage;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.given;

public class a_mocked_event_types_storage : Specification
{
    protected static readonly EventTypeId _eventTypeId = new("event-type-with-first-generation");
    protected static readonly EventTypeGeneration _firstGeneration = EventTypeGeneration.First;
    protected static readonly EventTypeGeneration _secondGeneration = new(2);

    protected IEventStoreDatabase _database;
    protected IMongoCollection<MongoEventType> _collection;
    protected List<MongoEventType> _eventTypesInDatabase;
    protected EventTypesStorage _storage;

    void Establish()
    {
        _database = Substitute.For<IEventStoreDatabase>();
        _collection = Substitute.For<IMongoCollection<MongoEventType>>();
        _database.GetCollection<MongoEventType>(WellKnownCollectionNames.EventTypes).Returns(_collection);

        _eventTypesInDatabase = [];

        _collection
            .FindAsync<MongoEventType>(Arg.Any<FilterDefinition<MongoEventType>>(), null, default)
            .Returns(_ =>
            {
                var cursor = Substitute.For<IAsyncCursor<MongoEventType>>();
                cursor.MoveNextAsync(default).Returns(true, false);
                cursor.Current.Returns(_ => [.. _eventTypesInDatabase]);
                return Task.FromResult(cursor);
            });

        _storage = new EventTypesStorage(EventStoreName.NotSet, _database, Substitute.For<ILogger<EventTypesStorage>>());
    }
}
