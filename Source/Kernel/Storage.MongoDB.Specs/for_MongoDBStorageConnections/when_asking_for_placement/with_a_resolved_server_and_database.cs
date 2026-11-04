// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBStorageConnections.when_asking_for_placement;

public class with_a_resolved_server_and_database : given.mongodb_storage_connections
{
    (string Scope, string Database) _result;

    void Establish()
    {
        _resolutionOptions.Connection.UseHttp(new Uri("https://platform.example/connections"));
        _resolver.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedConnection("mongodb://Tenant-Server:27017", "tenant-acme", Identity: new("acme-user", "acme-password")));
    }

    async Task Because() => _result = await _connections.Placement(StorageConnectionKind.Namespace, "orders+es+acme", "orders", "acme");

    [Fact] void should_place_it_on_the_resolved_server() => _result.Scope.ShouldEqual("tenant-server:27017");
    [Fact] void should_use_the_resolved_database() => _result.Database.ShouldEqual("tenant-acme");
    [Fact] void should_not_connect() => _clientManager.DidNotReceive().GetClientFor(Arg.Any<MongoClientSettings>());
}
