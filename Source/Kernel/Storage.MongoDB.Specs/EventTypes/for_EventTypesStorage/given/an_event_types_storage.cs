// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.for_EventTypesStorage.given;

public class an_event_types_storage : Specification
{
    protected static readonly EventTypeId _eventTypeId = new("event-type-with-first-generation");
    protected static readonly EventTypeGeneration _firstGeneration = EventTypeGeneration.First;
    protected static readonly EventTypeGeneration _secondGeneration = new(2);

    protected IEventStoreDatabase _database;
    protected IMongoCollection<EventType> _collection;
    protected List<EventType> _eventTypesInDatabase;
    protected EventTypesStorage _storage;

    void Establish()
    {
        _database = Substitute.For<IEventStoreDatabase>();
        _collection = Substitute.For<IMongoCollection<EventType>>();
        _database.GetCollection<EventType>(WellKnownCollectionNames.EventTypes).Returns(_collection);

        _eventTypesInDatabase = [];

        _collection
            .FindAsync<EventType>(Arg.Any<FilterDefinition<EventType>>(), null, default)
            .Returns(_ =>
            {
                var cursor = Substitute.For<IAsyncCursor<EventType>>();
                cursor.MoveNextAsync(default).Returns(true, false);
                cursor.Current.Returns(_ => [.. _eventTypesInDatabase]);
                return Task.FromResult(cursor);
            });

        _storage = new EventTypesStorage(EventStoreName.NotSet, _database, Substitute.For<ILogger<EventTypesStorage>>());
    }
}
