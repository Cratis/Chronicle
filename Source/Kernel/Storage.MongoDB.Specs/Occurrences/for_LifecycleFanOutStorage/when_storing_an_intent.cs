// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_LifecycleFanOutStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_storing_an_intent(ReplicaSetMongoDBFixture fixture) : LifecycleFanOutContract
{
    IMongoClient _client;
    IMongoDatabase _database;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(MongoDBSpecDatabaseNames.New());
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);

    protected override ILifecycleFanOutStorage OpenStorage() => new LifecycleFanOutStorage(_database);
}
