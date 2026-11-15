// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class when_a_label_or_type_is_not_in_the_definition : given.a_cypher_generator
{
    const string Hostile = "Person`) DETACH DELETE (x";

    Exception _nodeError;
    Exception _deleteError;
    Exception _edgeTypeError;
    Exception _endpointError;
    Exception _readError;

    void Because()
    {
        _nodeError = Catch.Exception(() => _cypher.WriteNode(new NodeReference(Hostile, "p1"), NoProperties, "owner", 1, 1));
        _deleteError = Catch.Exception(() => _cypher.DeleteNode(new NodeReference(Hostile, "p1"), true));
        _edgeTypeError = Catch.Exception(() => _cypher.DeleteEdge(new EdgeReference(Hostile, Person("p1"), Group("g1"))));
        _endpointError = Catch.Exception(() => _cypher.DeleteEdge(new EdgeReference("MEMBER_OF", new NodeReference(Hostile, "p1"), Group("g1"))));
        _readError = Catch.Exception(() => _cypher.ReadNodes(Hostile, 0, 10));
    }

    [Fact] void should_refuse_a_node_label() => _nodeError.ShouldBeOfExactType<UnknownGraphElement>();
    [Fact] void should_refuse_a_label_when_deleting() => _deleteError.ShouldBeOfExactType<UnknownGraphElement>();
    [Fact] void should_refuse_an_edge_type() => _edgeTypeError.ShouldBeOfExactType<UnknownGraphElement>();
    [Fact] void should_refuse_a_label_on_an_endpoint() => _endpointError.ShouldBeOfExactType<UnknownGraphElement>();
    [Fact] void should_refuse_a_label_when_reading() => _readError.ShouldBeOfExactType<UnknownGraphElement>();
}
