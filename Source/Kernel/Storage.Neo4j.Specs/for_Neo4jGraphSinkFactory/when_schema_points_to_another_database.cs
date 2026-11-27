// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jGraphSinkFactory;

public class when_schema_points_to_another_database : Specification
{
    IDriver _runtimeDriver;
    IDriver _schemaDriver;
    Neo4jSinkProvenanceStorage _provenance;
    Exception? _failure;

    void Establish()
    {
        _runtimeDriver = Substitute.For<IDriver>();
        _schemaDriver = Substitute.For<IDriver>();
        var connections = Substitute.For<INeo4jConnections>();
        connections.Options.Returns(new Neo4jOptions());
        var endpoint = new Uri("bolt://server:7687");
        connections.Resolve("store", "scope", ConnectionPurpose.Runtime).Returns(new Neo4jConnection(_runtimeDriver, "runtime", false) { Endpoint = endpoint });
        connections.Resolve("store", "scope", ConnectionPurpose.Schema).Returns(new Neo4jConnection(_schemaDriver, "redirected", true) { Endpoint = endpoint });
        _provenance = new(new Neo4jGraphTarget(connections, "store", "scope"));
    }

    async Task Because() => _failure = await Catch.Exception(_provenance.ReadState);

    [Fact] void should_refuse_split_placement() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_touch_the_runtime_database() => _runtimeDriver.DidNotReceive().AsyncSession(Arg.Any<Action<SessionConfigBuilder>>());
    [Fact] void should_not_create_or_use_the_redirected_database() => _schemaDriver.DidNotReceive().AsyncSession(Arg.Any<Action<SessionConfigBuilder>>());
}
