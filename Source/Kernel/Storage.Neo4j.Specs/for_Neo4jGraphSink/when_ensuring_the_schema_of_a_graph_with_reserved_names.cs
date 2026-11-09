// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jGraphSink;

public class when_ensuring_the_schema_of_a_graph_with_reserved_names : Specification
{
    INeo4jConnections _connections;
    Exception _property;
    Exception _key;

    void Establish()
    {
        _connections = Substitute.For<INeo4jConnections>();
        _connections.Options.Returns(new Neo4jOptions());
    }

    async Task Because()
    {
        _property = await Catch.Exception(() => Sink("personId", "_watermark").EnsureSchema());
        _key = await Catch.Exception(() => Sink("_namespace", "name").EnsureSchema());
    }

    Neo4jGraphSink Sink(string key, string property) =>
        new(new GraphDefinition("Reserved", [new("Person", key, GraphDefinitions.Schema((key, "string"), (property, "string")))], []), "Orders", "acme", _connections);

    [Fact] void should_refuse_a_property_named_like_the_bookkeeping() => _property.ShouldBeOfExactType<ReservedGraphPropertyName>();
    [Fact] void should_refuse_a_key_named_like_the_bookkeeping() => _key.ShouldBeOfExactType<ReservedGraphPropertyName>();
    [Fact] void should_not_connect_to_the_server() => _connections.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == nameof(INeo4jConnections.Resolve)).ShouldBeEmpty();
}
