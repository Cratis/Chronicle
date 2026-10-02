// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_database_connections.given;

public class all_dependencies : Specification
{
    protected string _server;
    protected string _prefix = "run_";
    protected bool? _directConnection = true;
    protected IMongoDBClientManager _clientManager;
    protected IMongoClient _client;
    protected List<MongoClientSettings> _settings;

    void Establish()
    {
        _settings = [];
        _client = Substitute.For<IMongoClient>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(Substitute.For<IMongoDatabase>());
        _clientManager = Substitute.For<IMongoDBClientManager>();
        _clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(call =>
        {
            _settings.Add(call.Arg<MongoClientSettings>());
            return _client;
        });
    }

    protected void Connect()
    {
        var options = Options.Create(new MongoDBOptions { Server = _server, DirectConnection = _directConnection });
        var storageOptions = Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix });
        _ = new EventStoreDatabase("Ada", _clientManager, options, storageOptions);
        _ = new EventStoreNamespaceDatabase("Ada", "tenant", _clientManager, options, storageOptions);
        var database = new Database(_clientManager, options, Substitute.For<ICustomSerializers>(), storageOptions);
        database.GetReadModelDatabase("Ada", "tenant");
    }
}
