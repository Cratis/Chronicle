// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jGraphSink;

public class when_a_label_targets_the_provenance_marker : Specification
{
    INeo4jConnections _connections;
    Exception? _literalError;
    Exception? _aliasError;

    void Establish()
    {
        _connections = Substitute.For<INeo4jConnections>();
        _connections.Options.Returns(new Neo4jOptions());
    }

    void Because()
    {
        _literalError = Catch.Exception(() => CreateSink(GraphBookkeeping.ProvenanceLabel));
        _aliasError = Catch.Exception(() => CreateSink(@"\u005f_cratis_sink_inventory"));
    }

    Neo4jGraphSink CreateSink(string label) =>
        new(new GraphDefinition("Graph", [new(label, "id", GraphDefinitions.Schema(("id", "string")))], []), "Orders", "acme", _connections);

    [Fact] void should_keep_the_literal_label_reserved() => _literalError.ShouldBeOfExactType<ReservedGraphPropertyName>();
    [Fact] void should_reject_the_unicode_escape_alias() => _aliasError.ShouldBeOfExactType<ArgumentException>();
}
