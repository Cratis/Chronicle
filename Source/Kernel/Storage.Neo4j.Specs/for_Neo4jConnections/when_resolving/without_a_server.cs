// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jConnections.when_resolving;

public class without_a_server : given.neo4j_connections
{
    Exception _error;

    protected override Neo4jOptions CreateOptions() => new();

    void Establish() => Configure();

    async Task Because() => _error = await Catch.Exception(async () => await _connections.Resolve("Orders", "acme", ConnectionPurpose.Runtime));

    [Fact] void should_fail_with_a_clear_error() => _error.ShouldBeOfExactType<MissingNeo4jServer>();
}
