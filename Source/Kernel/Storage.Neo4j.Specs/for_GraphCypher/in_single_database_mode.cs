// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class in_single_database_mode : Specification
{
    CypherStatement _node;
    CypherStatement _edge;
    CypherStatement _constraint;
    CypherStatement _ownerConstraint;
    CypherStatement _page;
    CypherStatement _bare;

    void Because()
    {
        var cypher = new GraphCypher(GraphDefinitions.Organization, "acme");
        var definition = GraphDefinitions.Organization;
        var props = new Dictionary<string, object?>();
        _node = cypher.WriteNode(new NodeReference("Person", "p1"), props, "owner", 1, 1);
        _edge = cypher.WriteEdge(new EdgeReference("MEMBER_OF", new NodeReference("Person", "p1"), new NodeReference("Group", "g1")), props, "owner", 1, 1);
        _constraint = cypher.CreateNodeConstraint(definition.Nodes[0]);
        _ownerConstraint = cypher.CreateOwnerConstraint();
        _page = cypher.ReadNodes("Person", 0, 10).Page;
        _bare = cypher.MakeNodeBare(new NodeReference("Person", "p1"));
    }

    [Fact] void should_identify_a_node_by_key_and_namespace() => _node.Text.StartsWith("MERGE (n:`Person` {`personId`: $key, `_namespace`: $namespace})", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_pass_the_namespace_as_a_parameter() => _node.Parameters["namespace"].ShouldEqual("acme");
    [Fact] void should_match_the_end_nodes_in_the_namespace() => _edge.Text.ShouldContain("`_namespace`: $namespace");
    [Fact] void should_stamp_the_namespace_on_the_edge() => _edge.Text.ShouldContain("r.`_namespace` = $namespace");
    [Fact] void should_make_the_constraint_composite() => _constraint.Text.ShouldContain("REQUIRE (n.`personId`, n.`_namespace`) IS UNIQUE");
    [Fact] void should_make_the_owner_constraint_composite() => _ownerConstraint.Text.ShouldContain("REQUIRE (o.`_owner`, o.`_namespace`) IS UNIQUE");
    [Fact] void should_filter_pages_on_the_namespace() => _page.Text.ShouldContain("WHERE n.`_namespace` = $namespace");
    [Fact] void should_keep_the_namespace_when_making_a_node_bare() => ((IDictionary<string, object>)_bare.Parameters["base"])["_namespace"].ShouldEqual("acme");
}
