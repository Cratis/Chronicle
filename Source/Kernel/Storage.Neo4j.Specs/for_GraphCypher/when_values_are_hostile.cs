// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class when_values_are_hostile : given.a_cypher_generator
{
    const string Hostile = "x'}) DETACH DELETE (m) //`";

    CypherStatement _node;
    CypherStatement _edge;
    CypherStatement _read;

    void Because()
    {
        _node = _cypher.WriteNode(Person(Hostile), Props(("name", Hostile), ("personId", Hostile)), Hostile, 1, 1);
        _edge = _cypher.WriteEdge(new EdgeReference("VISITED", Person(Hostile), Group(Hostile), Hostile), Props(("count", Hostile)), Hostile, 1, 1);
        _read = _cypher.ReadNode(Person(Hostile));
    }

    [Fact] void should_keep_the_value_out_of_the_node_text() => _node.Text.ShouldNotContain("DETACH");
    [Fact] void should_keep_the_value_out_of_the_edge_text() => _edge.Text.ShouldNotContain("DETACH");
    [Fact] void should_keep_the_value_out_of_the_read_text() => _read.Text.ShouldNotContain("DETACH");
    [Fact] void should_send_the_key_as_a_parameter() => _node.Parameters["key"].ShouldEqual(Hostile);
    [Fact] void should_send_the_identity_as_a_parameter() => _edge.Parameters["identity"].ShouldEqual(Hostile);
    [Fact] void should_send_the_owner_as_a_parameter() => _node.Parameters["owner"].ShouldEqual(Hostile);
}
