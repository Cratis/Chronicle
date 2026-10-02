// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_Database.given;

public class a_database : Specification
{
    protected Database _database;
    protected MongoDBOptions _options;
    protected MongoClientSettings _settings;
    protected IMongoClient _client;

    void Establish()
    {
        _options = new MongoDBOptions { Server = "mongodb://localhost:27017", DirectConnection = true };
        _client = Substitute.For<IMongoClient>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(Substitute.For<IMongoDatabase>());
        var clientManager = Substitute.For<IMongoDBClientManager>();
        clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(call =>
        {
            _settings = call.Arg<MongoClientSettings>();
            return _client;
        });
        _database = new Database(clientManager, Options.Create(_options), Substitute.For<ICustomSerializers>(), Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = "run_" }));
    }
}
