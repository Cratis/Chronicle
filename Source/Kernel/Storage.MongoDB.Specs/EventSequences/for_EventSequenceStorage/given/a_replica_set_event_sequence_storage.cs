// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public abstract class a_replica_set_event_sequence_storage(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected EventSequenceStorage _storage;
    protected EventType _eventType;

    IMongoClient _client;
    string _databaseName;

    void Establish()
    {
        _eventType = new EventType("some-event", EventTypeGeneration.First);
        _databaseName = $"chronicle_event_sequence_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var collection = database.GetCollection<Event>("event-log");

        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.Client.Returns(_client);
        namespaceDatabase.GetEventSequenceCollectionFor(Arg.Any<EventSequenceId>()).Returns(collection);
        namespaceDatabase.GetEventSequenceCollectionAsBsonFor(Arg.Any<EventSequenceId>()).Returns(database.GetCollection<BsonDocument>("event-log"));

        var eventTypesStorage = Substitute.For<IEventTypesStorage>();
        eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.None, EventTypeSource.Unknown, new JsonSchema()));

        var identityStorage = Substitute.For<IIdentityStorage>();
        identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);

        var expandoObjectConverter = Substitute.For<Json.IExpandoObjectConverter>();
        expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());

        _storage = new EventSequenceStorage(
            "test-store",
            "test-namespace",
            EventSequenceId.Log,
            namespaceDatabase,
            Substitute.For<IEventConverter>(),
            eventTypesStorage,
            identityStorage,
            expandoObjectConverter,
            new JsonSerializerOptions(),
            Substitute.For<ILogger<EventSequenceStorage>>());
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    protected EventToAppendToStorage EventAt(EventSequenceNumber sequenceNumber) => EventAt(sequenceNumber, _eventType);

    protected EventToAppendToStorage EventAt(EventSequenceNumber sequenceNumber, EventType eventType) => new(
        sequenceNumber,
        EventSourceType.Default,
        "some-source",
        EventStreamType.All,
        EventStreamId.Default,
        eventType,
        CorrelationId.NotSet,
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        new ExpandoObject(),
        EventHash.NotSet);
}
