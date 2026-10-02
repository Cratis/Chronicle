// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBKernelStateResetHandler.given;

public class a_reset_handler : Specification
{
    protected IMongoClient _client;
    protected MongoDBStorageOptions _storageOptions;
    protected MongoDBKernelStateResetHandler _handler;

    void Establish()
    {
        _client = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(database);
        var cursor = Substitute.For<IAsyncCursor<string>>();
        cursor.Current.Returns(["admin", "config", "local", "chronicle+main", "Testing+es+default", "run_chronicle+main", "run_Testing+es+default", "other_Testing+es+default", "RUN_Testing+es+default"]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _client.ListDatabaseNamesAsync(Arg.Any<CancellationToken>()).Returns(cursor);
        var clientManager = Substitute.For<IMongoDBClientManager>();
        clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        _storageOptions = new MongoDBStorageOptions();
        _handler = new MongoDBKernelStateResetHandler(
            Options.Create(new ChronicleOptions()),
            Options.Create(new MongoDBOptions { Server = "mongodb://localhost:27017" }),
            clientManager,
            Options.Create(_storageOptions));
    }
}
