// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jGraphSinkFactory;

public class when_runtime_is_moved_after_binding : Specification
{
    Neo4jGraphTarget _target;
    Neo4jGraphSink _graph;
    IDriver _redirected;
    Exception? _failure;

    void Establish()
    {
        var connections = Substitute.For<INeo4jConnections>();
        connections.Options.Returns(new Neo4jOptions());
        _redirected = Substitute.For<IDriver>();
        var first = new Neo4jConnection(Substitute.For<IDriver>(), "neo4j", false) { Endpoint = new("bolt://first:7687") };
        var second = new Neo4jConnection(_redirected, "neo4j", false) { Endpoint = new("bolt://second:7687") };
        connections.Resolve("store", "scope", ConnectionPurpose.Runtime).Returns(first, second);
        _target = new(connections, "store", "scope");
        _graph = new(GraphDefinitions.Organization, "store", "scope", connections, _target);
    }

    async Task Because()
    {
        await _target.Runtime();
        _failure = await Catch.Exception(async () => await _graph.FindNode("owner", new NodeReference("Person", "person")));
    }

    [Fact] void should_refuse_the_redirected_read() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_open_a_session_on_the_redirected_server() => _redirected.DidNotReceive().AsyncSession(Arg.Any<Action<SessionConfigBuilder>>());
}
