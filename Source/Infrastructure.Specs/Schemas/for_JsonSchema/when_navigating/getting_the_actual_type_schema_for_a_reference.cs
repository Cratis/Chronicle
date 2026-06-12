// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_navigating;

public class getting_the_actual_type_schema_for_a_reference : Specification
{
    const string Json = """
    {
        "type": "object",
        "$defs": {
            "Inner": { "type": "object", "title": "Inner", "properties": { "value": { "type": "integer" } } }
        },
        "properties": { "inner": { "$ref": "#/$defs/Inner" } }
    }
    """;

    JsonSchema _schema;
    JsonSchema _actual;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _actual = _schema.ActualProperties["inner"].ActualTypeSchema;

    [Fact] void should_follow_the_reference_to_the_actual_schema() => _actual.Title.ShouldEqual("Inner");
    [Fact] void should_expose_the_resolved_properties() => _actual.ActualProperties.Keys.ShouldContain("value");
}
