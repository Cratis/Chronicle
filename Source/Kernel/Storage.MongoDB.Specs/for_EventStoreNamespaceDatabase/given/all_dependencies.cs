// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceDatabase.given;

public class all_dependencies : Specification
{
    protected IMongoDBClientManager _clientManager;
    protected IOptions<MongoDBOptions> _options;
    protected IOptions<MongoDBStorageOptions> _storageOptions;
    protected MongoClientSettings _settings;
    protected IMongoClient _client;

    void Establish()
    {
        _options = Options.Create(new MongoDBOptions { DirectConnection = true });
        _storageOptions = Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = "run_" });
        _client = Substitute.For<IMongoClient>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(Substitute.For<IMongoDatabase>());
        _clientManager = Substitute.For<IMongoDBClientManager>();
        _clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(call =>
        {
            _settings = call.Arg<MongoClientSettings>();
            return _client;
        });
    }
}
