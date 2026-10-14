// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public abstract class a_state_storage(MongoDBFixture fixture) : an_event_sequence_storage
{
    protected IMongoCollection<BsonDocument> _stateDocuments;
    IMongoClient _client;
    string _databaseName;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        var database = _client.GetDatabase(_databaseName);
        _stateDocuments = database.GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences);
        _database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences)
            .Returns(database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences));
    }

    Task Destroy() => _client.DropDatabaseAsync(_databaseName);
}
