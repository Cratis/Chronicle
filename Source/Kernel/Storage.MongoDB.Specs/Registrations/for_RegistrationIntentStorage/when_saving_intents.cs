// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Registrations;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Registrations.for_RegistrationIntentStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_saving_intents(ReplicaSetMongoDBFixture fixture) : RegistrationIntentStorageContract
{
    IMongoClient _client;
    IMongoDatabase _database;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(MongoDBSpecDatabaseNames.New());
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);

    protected override IRegistrationIntentStorage OpenStorage() => new RegistrationIntentStorage(_database);
}
