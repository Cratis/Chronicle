// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class when_writing_a_node : given.a_cypher_generator
{
    CypherStatement _statement;

    void Because() => _statement = _cypher.WriteNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada")), "owner", 7, 3);

    [Fact] void should_merge_on_the_key_of_the_definition() => _statement.Text.StartsWith("MERGE (n:`Person` {`personId`: $key})", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_set_the_properties_from_a_parameter() => _statement.Text.ShouldContain("SET n += $properties");
    [Fact] void should_set_the_watermark_from_the_sequence() => _statement.Text.ShouldContain("n.`_watermark` = $sequence");
    [Fact] void should_only_claim_an_unowned_or_own_node() => _statement.Text.ShouldContain("WHERE n.`_owner` IS NULL OR n.`_owner` = $owner");
    [Fact] void should_stamp_the_owner_and_the_generation() => _statement.Text.ShouldContain("SET n.`_owner` = $owner, n.`_generation` = $generation");
    [Fact] void should_pass_the_key_as_a_parameter() => _statement.Parameters["key"].ShouldEqual("p1");
    [Fact] void should_pass_the_sequence_as_a_parameter() => _statement.Parameters["sequence"].ShouldEqual(7L);
    [Fact] void should_pass_the_owner_as_a_parameter() => _statement.Parameters["owner"].ShouldEqual("owner");
    [Fact] void should_not_pass_the_key_property_again_as_a_property() => ((IDictionary<string, object>)_statement.Parameters["properties"]).ContainsKey("personId").ShouldBeFalse();
    [Fact] void should_pass_the_other_properties() => ((IDictionary<string, object>)_statement.Parameters["properties"])["name"].ShouldEqual("Ada");
    [Fact] void should_not_mention_a_value_in_the_text() => _statement.Text.ShouldNotContain("Ada");
}
