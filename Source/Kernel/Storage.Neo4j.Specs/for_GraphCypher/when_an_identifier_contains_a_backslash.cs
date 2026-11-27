// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphCypher;

public class when_an_identifier_contains_a_backslash : Specification
{
    const string Label = @"Other\Label";
    const string Type = @"RELATED\TO";
    const string Key = @"person\id";

    Exception? _labelError;
    Exception? _typeError;
    Exception? _propertyError;

    void Because()
    {
        var cypher = new GraphCypher(
            new GraphDefinition(
                "People",
                [new(Label, "id", GraphDefinitions.Schema(("id", "string"))), new("Person", Key, GraphDefinitions.Schema((Key, "string")))],
                [new(Type, "Person", "Person", null, GraphDefinitions.Schema())]),
            null);
        var person = new NodeReference("Person", "person");
        _labelError = Catch.Exception(() => cypher.ReadNode(new NodeReference(Label, "other")));
        _typeError = Catch.Exception(() => cypher.SweepEdges("owner", 1).ToArray());
        _propertyError = Catch.Exception(() => cypher.ReadNode(person));
    }

    [Fact] void should_reject_the_label() => _labelError.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_the_relationship_type() => _typeError.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_the_key_property() => _propertyError.ShouldBeOfExactType<ArgumentException>();
}
