// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class when_creating_the_schema : given.a_cypher_generator
{
    CypherStatement _constraint;
    CypherStatement _ownerConstraint;
    CypherStatement _index;

    void Because()
    {
        var definition = GraphDefinitions.Organization;
        _constraint = _cypher.CreateNodeConstraint(definition.Nodes[0]);
        _ownerConstraint = _cypher.CreateOwnerConstraint();
        _index = _cypher.CreateEdgeIndex(definition.Edges[1]);
    }

    [Fact] void should_create_a_uniqueness_constraint_if_not_exists() =>
        _constraint.Text.ShouldEqual("CREATE CONSTRAINT `chronicle_node_Person_personId` IF NOT EXISTS FOR (n:`Person`) REQUIRE n.`personId` IS UNIQUE");
    [Fact] void should_create_the_owner_constraint_if_not_exists() => _ownerConstraint.Text.ShouldContain("IF NOT EXISTS FOR (o:`_Owner`) REQUIRE o.`_owner` IS UNIQUE");
    [Fact] void should_create_the_index_of_the_identifying_property_if_not_exists() =>
        _index.Text.ShouldEqual("CREATE INDEX `chronicle_edge_VISITED_visitId` IF NOT EXISTS FOR ()-[r:`VISITED`]-() ON (r.`visitId`)");
}
