// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_TriggerDefinitionsStorage.given;

public abstract class trigger_definitions(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected readonly TriggerDefinition _definition = new(new TriggerId("payment-import"), "Importer", "{}", 1);
    protected ITriggerDefinitionsStorage _storage;
    protected IMongoDatabase _database;
    IMongoClient _client;
    string _databaseName;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_databaseName);
        _storage = new TriggerDefinitionsStorage(_database);
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
