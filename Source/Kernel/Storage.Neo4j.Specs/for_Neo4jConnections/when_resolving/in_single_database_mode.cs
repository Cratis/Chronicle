// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jConnections.when_resolving;

public class in_single_database_mode : given.neo4j_connections
{
    Neo4jConnection _first;
    Neo4jConnection _second;
    Neo4jConnection _named;

    protected override Neo4jOptions CreateOptions() => new() { Server = "bolt://configured:7687", DatabaseMode = Neo4jDatabaseMode.Single };

    void Establish() => Configure();

    async Task Because()
    {
        _first = await _connections.Resolve("Orders", "acme", ConnectionPurpose.Runtime);
        _second = await _connections.Resolve("Shipping", "globex", ConnectionPurpose.Runtime);
        _neo4j = new() { Server = "bolt://configured:7687", DatabaseMode = Neo4jDatabaseMode.Single, SingleDatabase = "graph" };
        Configure();
        _named = await _connections.Resolve("Orders", "acme", ConnectionPurpose.Runtime);
    }

    [Fact] void should_use_the_default_single_database() => _first.Database.ShouldEqual("neo4j");
    [Fact] void should_use_the_same_database_for_every_event_store_and_namespace() => _second.Database.ShouldEqual("neo4j");
    [Fact] void should_use_the_configured_single_database() => _named.Database.ShouldEqual("graph");
    [Fact] void should_not_create_the_database() => _first.CanCreateDatabase.ShouldBeFalse();
}
