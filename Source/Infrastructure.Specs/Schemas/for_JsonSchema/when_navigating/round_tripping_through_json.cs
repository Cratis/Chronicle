// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_navigating;

public class round_tripping_through_json : Specification
{
    const string Json = """
    { "type": "object", "title": "Sample", "properties": { "name": { "type": "string" } } }
    """;

    JsonSchema _schema;
    JsonSchema _roundTripped;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _roundTripped = JsonSchema.FromJson(_schema.ToJson());

    [Fact] void should_preserve_the_title() => _roundTripped.Title.ShouldEqual("Sample");
    [Fact] void should_preserve_the_properties() => _roundTripped.ActualProperties.Keys.ShouldContain("name");
    [Fact] void should_produce_equivalent_json() =>
        JsonNode.DeepEquals(JsonNode.Parse(_schema.ToJson()), JsonNode.Parse(_roundTripped.ToJson())).ShouldBeTrue();
}
