// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jConnections.when_resolving;

public class with_a_prefix : given.neo4j_connections
{
    Neo4jConnection _connection;
    Neo4jConnection _withoutCreating;

    protected override Neo4jOptions CreateOptions() => new() { Server = "bolt://configured:7687", DatabasePrefix = "cratis" };

    void Establish() => Configure();

    async Task Because()
    {
        _connection = await _connections.Resolve("Orders", "acme", ConnectionPurpose.Runtime);
        _neo4j = new() { Server = "bolt://configured:7687", CreateIfMissing = false };
        Configure();
        _withoutCreating = await _connections.Resolve("Orders", "acme", ConnectionPurpose.Schema);
    }

    [Fact] void should_prefix_the_database() => _connection.Database.ShouldEqual("cratis-orders-acme");
    [Fact] void should_not_create_the_database_when_told_not_to() => _withoutCreating.CanCreateDatabase.ShouldBeFalse();
}
