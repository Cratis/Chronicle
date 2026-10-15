// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBStorageConnections.given;

public class mongodb_storage_connections : Specification
{
    protected MongoDBStorageOptions _storageOptions;
    protected ICachingConnectionResolver _resolver;
    protected IConnectionResolverFactory _resolverFactory;
    protected IMongoDBClientManager _clientManager;
    protected IMongoClient _client;
    protected MongoClientSettings? _settings;
    protected MongoDBStorageConnections _connections;

    void Establish()
    {
        _storageOptions = new();
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>()).Returns(ResolvedConnection.Unchanged);
        _resolverFactory = Substitute.For<IConnectionResolverFactory>();
        _resolverFactory.Create(Arg.Any<ConnectionResolutionOptions>()).Returns(_resolver);
        _client = Substitute.For<IMongoClient>();
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(call =>
        {
            var database = Substitute.For<IMongoDatabase>();
            database.DatabaseNamespace.Returns(new DatabaseNamespace(call.Arg<string>()));
            return database;
        });
        _clientManager = Substitute.For<IMongoDBClientManager>();
        _clientManager.GetClientFor(Arg.Do<MongoClientSettings>(_ => _settings = _)).Returns(_client);
        _connections = new(
            Options.Create(new MongoDBOptions { Server = "mongodb://configured:27017", Database = "chronicle" }),
            Options.Create(_storageOptions),
            _clientManager,
            _resolverFactory);
    }
}
