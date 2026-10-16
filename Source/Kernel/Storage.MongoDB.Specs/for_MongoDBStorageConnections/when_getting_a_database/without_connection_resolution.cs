// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBStorageConnections.when_getting_a_database;

public class without_connection_resolution : given.mongodb_storage_connections
{
    IMongoDatabase _result;

    void Because() => _result = _connections.GetDatabase(StorageConnectionKind.Namespace, "orders+es+acme", "orders", "acme");

    [Fact] void should_use_the_derived_name() => _result.DatabaseNamespace.DatabaseName.ShouldEqual("orders+es+acme");
    [Fact] void should_connect_to_the_configured_server() => _settings!.Server.Host.ShouldEqual("configured");
    [Fact] void should_not_resolve_anything() => _resolver.DidNotReceive().Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
}
