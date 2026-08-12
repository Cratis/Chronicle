// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_a_nested_object_property_has_a_wrong_value_type : Specification
{
    const string SchemaJson = """
        {
            "type": "object",
            "properties": {
                "outer": {
                    "type": "object",
                    "properties": { "inner": { "type": "integer" } }
                }
            }
        }
        """;

    IList<JsonSchemaValidationError> _result;

    void Because() => _result = JsonSchema.FromJson(SchemaJson).Validate("""{"outer":{"inner":"not-an-integer"}}""");

    [Fact] void should_report_a_single_error() => _result.Count.ShouldEqual(1);
    [Fact] void should_report_a_wrong_property_type() => _result.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.WrongPropertyType);
    [Fact] void should_report_the_dotted_path_to_the_nested_property() => _result.Single().Path.ShouldEqual("outer.inner");
}
