// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class when_writing_an_edge : given.a_cypher_generator
{
    CypherStatement _plain;
    CypherStatement _identified;
    CypherStatement _deleted;
    CypherStatement _read;

    void Because()
    {
        _plain = _cypher.WriteEdge(new EdgeReference("MEMBER_OF", Person("p1"), Group("g1")), Props(("since", "2020")), "owner", 3, 1);
        _identified = _cypher.WriteEdge(new EdgeReference("VISITED", Person("p1"), Group("g1"), "v1"), Props(("visitId", "v1"), ("count", 2L)), "owner", 3, 1);
        _deleted = _cypher.DeleteEdge(new EdgeReference("VISITED", Person("p1"), Group("g1"), "v1"));
        _read = _cypher.ReadEdge(new EdgeReference("MEMBER_OF", Person("p1"), Group("g1")));
    }

    [Fact] void should_match_both_end_nodes_by_their_keys() => _plain.Text.StartsWith("MATCH (s:`Person` {`personId`: $sourceKey}) MATCH (t:`Group` {`groupId`: $targetKey})", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_merge_the_relationship_on_type_alone_without_an_identity() => _plain.Text.ShouldContain("MERGE (s)-[r:`MEMBER_OF`]->(t)");
    [Fact] void should_merge_the_relationship_on_the_identifying_property() => _identified.Text.ShouldContain("MERGE (s)-[r:`VISITED` {`visitId`: $identity}]->(t)");
    [Fact] void should_pass_the_identity_as_a_parameter() => _identified.Parameters["identity"].ShouldEqual("v1");
    [Fact] void should_not_pass_the_identity_property_again() => ((IDictionary<string, object>)_identified.Parameters["properties"]).ContainsKey("visitId").ShouldBeFalse();
    [Fact] void should_delete_the_relationship_only() => _deleted.Text.EndsWith("DELETE r").ShouldBeTrue();
    [Fact] void should_filter_a_delete_on_the_identity() => _deleted.Text.ShouldContain("WHERE r.`visitId` = $identity");
    [Fact] void should_read_one_relationship() => _read.Text.EndsWith("RETURN properties(r) AS properties LIMIT 1", StringComparison.Ordinal).ShouldBeTrue();
}
