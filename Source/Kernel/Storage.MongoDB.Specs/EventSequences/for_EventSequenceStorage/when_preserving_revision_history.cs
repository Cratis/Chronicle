// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_preserving_revision_history(ReplicaSetMongoDBFixture fixture) : EventRevisionContract
{
    readonly string _databaseName = MongoDBSpecDatabaseNames.New();
    IMongoClient? _client;

    protected override Task<IEventSequenceStorage> OpenStorage()
    {
        _client ??= new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var collection = database.GetCollection<Event>("event-log");
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.Client.Returns(_client);
        namespaceDatabase.GetEventSequenceCollectionFor(Arg.Any<EventSequenceId>()).Returns(collection);
        namespaceDatabase.GetEventSequenceCollectionAsBsonFor(Arg.Any<EventSequenceId>()).Returns(database.GetCollection<BsonDocument>("event-log"));

        var converter = new Json.ExpandoObjectConverter(new Schemas.TypeFormats());
        return Task.FromResult<IEventSequenceStorage>(new EventSequenceStorage(
            "test-store",
            "test-namespace",
            EventSequenceId.Log,
            namespaceDatabase,
            new EventConverter("test-store", "test-namespace", EventTypes, Identities, converter),
            EventTypes,
            Identities,
            converter,
            new JsonSerializerOptions(),
            Substitute.For<ILogger<EventSequenceStorage>>()));
    }

    async Task Destroy()
    {
        if (_client is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }
}
