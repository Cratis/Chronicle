// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jConnections.when_resolving;

public class without_a_connection_resolution : given.neo4j_connections
{
    Neo4jConnection _connection;
    IAuthToken _token;
    Neo4jConnection _defaultNamespace;

    void Establish() => Configure();

    async Task Because()
    {
        _connection = await _connections.Resolve("Orders", "Acme Corp", ConnectionPurpose.Runtime);
        _token = await _authentication.GetTokenAsync();
        _defaultNamespace = await _connections.Resolve("Orders", "Default", ConnectionPurpose.Runtime);
    }

    [Fact] void should_use_the_configured_server() => _server.ShouldEqual(new Uri("bolt://configured:7687"));
    [Fact] void should_use_the_derived_database_name() => _connection.Database.ShouldEqual("orders-acme-corp-fd6ee09c");
    [Fact] void should_use_the_bare_store_name_for_the_default_namespace() => _defaultNamespace.Database.ShouldEqual("orders");
    [Fact] void should_authenticate_with_the_user_and_the_secret_from_the_configuration() => _token.ShouldEqual(AuthTokens.Basic("chronicle", "the-secret"));
    [Fact] void should_use_the_driver_of_the_factory() => _connection.Driver.ShouldEqual(_driver);
    [Fact] void should_allow_creating_the_database() => _connection.CanCreateDatabase.ShouldBeTrue();
    [Fact] void should_not_ask_a_resolver() => _request.ShouldBeNull();
}
