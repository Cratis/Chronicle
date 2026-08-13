// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_a_referenced_schema_has_a_property_with_a_wrong_value_type : Specification
{
    const string SchemaJson = """
        {
            "type": "object",
            "properties": {
                "address": { "$ref": "#/$defs/Address" },
                "previousAddresses": { "type": "array", "items": { "$ref": "#/$defs/Address" } }
            },
            "$defs": {
                "Address": {
                    "type": "object",
                    "properties": { "zipCode": { "type": "integer" } }
                }
            }
        }
        """;

    IList<JsonSchemaValidationError> _throughAReferencedProperty;
    IList<JsonSchemaValidationError> _throughAReferencedArrayItem;
    IList<JsonSchemaValidationError> _forValidContent;

    void Because()
    {
        var schema = JsonSchema.FromJson(SchemaJson);
        _throughAReferencedProperty = schema.Validate("""{"address":{"zipCode":"not-an-integer"}}""");
        _throughAReferencedArrayItem = schema.Validate("""{"previousAddresses":[{"zipCode":1234},{"zipCode":"nope"}]}""");
        _forValidContent = schema.Validate("""{"address":{"zipCode":1234}}""");
    }

    [Fact] void should_resolve_the_reference_and_report_the_dotted_path() => _throughAReferencedProperty.Single().Path.ShouldEqual("address.zipCode");
    [Fact] void should_report_a_wrong_property_type() => _throughAReferencedProperty.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.WrongPropertyType);
    [Fact] void should_resolve_the_reference_through_array_items() => _throughAReferencedArrayItem.Single().Path.ShouldEqual("previousAddresses[1].zipCode");
    [Fact] void should_report_nothing_for_valid_content() => _forValidContent.ShouldBeEmpty();
}
