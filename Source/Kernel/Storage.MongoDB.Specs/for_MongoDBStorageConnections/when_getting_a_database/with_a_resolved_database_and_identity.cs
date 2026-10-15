// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBStorageConnections.when_getting_a_database;

public class with_a_resolved_database_and_identity : given.mongodb_storage_connections
{
    ConnectionRequest? _request;
    IMongoDatabase _result;

    void Establish()
    {
        _storageOptions.ConnectionResolution.Connection.UseHttp(new Uri("https://platform.example/connections"));
        _resolver.Resolve(Arg.Do<ConnectionRequest>(_ => _request = _), Arg.Any<CancellationToken>())
            .Returns(new ResolvedConnection("mongodb://tenant-server:27017", "tenant-acme", Identity: new("acme-user", "acme-password")));
    }

    void Because() => _result = _connections.GetDatabase(StorageConnectionKind.Namespace, "orders+es+acme", "orders", "acme");

    [Fact] void should_resolve_for_mongodb() => _request!.Storage.ShouldEqual(WellKnownConnectionStorage.MongoDB);
    [Fact] void should_resolve_the_namespace_kind() => _request!.Scope[StorageConnectionRequests.Kind].ShouldEqual("namespace");
    [Fact] void should_resolve_for_the_event_store() => _request!.Scope[StorageConnectionRequests.EventStore].ShouldEqual("orders");
    [Fact] void should_resolve_for_the_namespace() => _request!.Scope[StorageConnectionRequests.Namespace].ShouldEqual("acme");
    [Fact] void should_use_the_resolved_database() => _result.DatabaseNamespace.DatabaseName.ShouldEqual("tenant-acme");
    [Fact] void should_connect_to_the_resolved_server() => _settings!.Server.Host.ShouldEqual("tenant-server");
    [Fact] void should_authenticate_as_the_resolved_user() => _settings!.Credential.Username.ShouldEqual("acme-user");
}
