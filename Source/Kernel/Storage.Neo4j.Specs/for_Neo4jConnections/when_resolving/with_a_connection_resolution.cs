// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jConnections.when_resolving;

public class with_a_connection_resolution : given.neo4j_connections
{
    Neo4jConnection _connection;
    ConnectionRequest? _runtimeRequest;
    ConnectionRequest? _schemaRequest;
    IAuthToken _token;

    void Establish()
    {
        _resolutionOptions.Connection.UseHttp(new Uri("https://platform.example/connections"));
        _resolved = new("bolt://tenant-7:7687", "tenant_acme", Identity: new("acme-user", "acme-password"));
        Configure();
    }

    async Task Because()
    {
        _connection = await _connections.Resolve("Orders", "acme", ConnectionPurpose.Runtime);
        _runtimeRequest = _request;
        _token = await _authentication.GetTokenAsync();
        await _connections.Resolve("Orders", "acme", ConnectionPurpose.Schema);
        _schemaRequest = _request;
    }

    [Fact] void should_ask_for_the_neo4j_storage() => _runtimeRequest!.Storage.ShouldEqual("Neo4j");
    [Fact] void should_ask_for_the_runtime_purpose() => _runtimeRequest!.Purpose.ShouldEqual(ConnectionPurpose.Runtime);
    [Fact] void should_ask_for_the_schema_purpose() => _schemaRequest!.Purpose.ShouldEqual(ConnectionPurpose.Schema);
    [Fact] void should_scope_the_request_to_read_models() => _runtimeRequest!.Scope[StorageConnectionRequests.Kind].ShouldEqual("readModels");
    [Fact] void should_scope_the_request_to_the_event_store() => _runtimeRequest!.Scope[StorageConnectionRequests.EventStore].ShouldEqual("Orders");
    [Fact] void should_scope_the_request_to_the_namespace() => _runtimeRequest!.Scope[StorageConnectionRequests.Namespace].ShouldEqual("acme");
    [Fact] void should_connect_to_the_resolved_bolt_uri() => _server.ShouldEqual(new Uri("bolt://tenant-7:7687"));
    [Fact] void should_use_the_resolved_database() => _connection.Database.ShouldEqual("tenant_acme");
    [Fact] void should_not_create_a_database_the_platform_placed() => _connection.CanCreateDatabase.ShouldBeFalse();
    [Fact] void should_follow_the_resolution_for_credentials() => _authentication.ShouldBeOfExactType<Neo4jConnectionResolutionHooks>();
    [Fact] void should_authenticate_with_the_resolved_identity() => _token.ShouldEqual(AuthTokens.Basic("acme-user", "acme-password"));
}
