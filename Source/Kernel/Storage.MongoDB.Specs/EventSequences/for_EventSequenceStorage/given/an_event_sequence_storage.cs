// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public class an_event_sequence_storage : Specification
{
    protected IEventStoreNamespaceDatabase _database;
    protected IMongoCollection<Event> _collection;
    protected IEventConverter _converter;
    protected IEventTypesStorage _eventTypesStorage;
    protected IIdentityStorage _identityStorage;
    protected Json.IExpandoObjectConverter _expandoObjectConverter;
    protected EventSequenceStorage _storage;
    protected EventStoreName _eventStoreName;
    protected EventStoreNamespaceName _namespaceName;
    protected EventSequenceId _eventSequenceId;

    void Establish()
    {
        _eventStoreName = new EventStoreName("test-event-store");
        _namespaceName = new EventStoreNamespaceName("test-namespace");
        _eventSequenceId = EventSequenceId.Log;
        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _collection = Substitute.For<IMongoCollection<Event>>();
        _database.GetEventSequenceCollectionFor(_eventSequenceId)
            .Returns(_collection);

        _converter = Substitute.For<IEventConverter>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _identityStorage = Substitute.For<IIdentityStorage>();
        _expandoObjectConverter = Substitute.For<Json.IExpandoObjectConverter>();

        var jsonSerializerOptions = new JsonSerializerOptions();
        var logger = Substitute.For<ILogger<EventSequenceStorage>>();

        _storage = new EventSequenceStorage(
            _eventStoreName,
            _namespaceName,
            _eventSequenceId,
            _database,
            _converter,
            _eventTypesStorage,
            _identityStorage,
            _expandoObjectConverter,
            jsonSerializerOptions,
            logger);
    }
}
