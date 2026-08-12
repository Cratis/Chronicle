// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_an_array_item_has_a_wrong_value_type : Specification
{
    const string SchemaJson = """
        {
            "type": "object",
            "properties": {
                "tags": { "type": "array", "items": { "type": "string" } }
            }
        }
        """;

    IList<JsonSchemaValidationError> _result;
    IList<JsonSchemaValidationError> _whenTheArrayItselfIsWrong;

    void Because()
    {
        var schema = JsonSchema.FromJson(SchemaJson);
        _result = schema.Validate("""{"tags":["first",42,"third"]}""");
        _whenTheArrayItselfIsWrong = schema.Validate("""{"tags":"first"}""");
    }

    [Fact] void should_report_a_single_error_for_the_item() => _result.Count.ShouldEqual(1);
    [Fact] void should_report_the_indexed_path_of_the_offending_item() => _result.Single().Path.ShouldEqual("tags[1]");
    [Fact] void should_report_a_wrong_property_type_for_the_item() => _result.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.WrongPropertyType);
    [Fact] void should_report_the_array_property_itself_when_it_is_not_an_array() => _whenTheArrayItselfIsWrong.Single().Path.ShouldEqual("tags");
}
