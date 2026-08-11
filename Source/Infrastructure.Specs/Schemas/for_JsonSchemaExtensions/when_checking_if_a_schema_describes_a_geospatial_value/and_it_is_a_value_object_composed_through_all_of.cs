// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_a_geospatial_value;

/// <summary>
/// Inheritance is expressed as an <c>allOf</c> reference to the base beside the type's own properties, and
/// <c>ActualTypeSchema</c> resolves such a schema to the base — dropping the properties the type itself declares.
/// A predicate that answered on the resolved schema would call this a single typed value and let a compliance
/// walk skip members that are declared right there, so the answer has to come from the format and nothing else.
/// </summary>
public class and_it_is_a_value_object_composed_through_all_of : Specification
{
    const string Schema = """
        {
          "type": "object",
          "$defs": { "Base": { "type": "object" } },
          "allOf": [ { "$ref": "#/$defs/Base" } ],
          "properties": { "name": { "type": "string" } }
        }
        """;

    JsonSchema _schema;
    bool _result;

    void Establish() => _schema = JsonSchema.FromJson(Schema);

    void Because() => _result = _schema.DescribesGeospatialValue();

    [Fact] void should_not_describe_a_geospatial_value() => _result.ShouldBeFalse();
    [Fact] void should_still_declare_its_own_property() => _schema.GetFlattenedProperties().Select(_ => _.Name).ShouldContain("name");
}
