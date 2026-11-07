// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class when_the_definition_has_backticks_in_a_name : Specification
{
    CypherStatement _statement;
    string _escaped;

    void Because()
    {
        var cypher = new GraphCypher(
            new GraphDefinition("odd", [new("We`ird", "ke`y", GraphDefinitions.Schema(("ke`y", "string")))], []),
            null);
        _statement = cypher.ReadNode(new NodeReference("We`ird", "k"));
        _escaped = GraphCypher.Escape("a`b");
    }

    [Fact] void should_double_the_backtick_in_the_label() => _statement.Text.ShouldContain("`We``ird`");
    [Fact] void should_double_the_backtick_in_the_key_property() => _statement.Text.ShouldContain("`ke``y`");
    [Fact] void should_escape_an_identifier() => _escaped.ShouldEqual("`a``b`");
}
