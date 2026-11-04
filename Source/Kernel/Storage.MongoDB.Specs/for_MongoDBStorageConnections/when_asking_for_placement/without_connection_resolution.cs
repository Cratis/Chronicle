// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBStorageConnections.when_asking_for_placement;

public class without_connection_resolution : given.mongodb_storage_connections
{
    (string Scope, string Database) _result;

    async Task Because() => _result = await _connections.Placement(StorageConnectionKind.Namespace, "orders+es+acme", "orders", "acme");

    [Fact] void should_place_it_on_the_configured_server() => _result.Scope.ShouldEqual("configured:27017");
    [Fact] void should_use_the_derived_name() => _result.Database.ShouldEqual("orders+es+acme");
    [Fact] void should_not_connect() => _clientManager.DidNotReceive().GetClientFor(Arg.Any<MongoClientSettings>());
    [Fact] void should_not_resolve_anything() => _resolver.DidNotReceive().Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
}
