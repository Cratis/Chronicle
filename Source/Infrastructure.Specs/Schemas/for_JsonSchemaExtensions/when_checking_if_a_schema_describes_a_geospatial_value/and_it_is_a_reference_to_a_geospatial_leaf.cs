// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_a_geospatial_value;

/// <summary>
/// A type used more than once is emitted once under <c>$defs</c> and referenced, so the format sits on the
/// definition rather than on the property that points at it.
/// </summary>
public class and_it_is_a_reference_to_a_geospatial_leaf : Specification
{
    const string Schema = """
        {
          "type": "object",
          "$defs": { "Point": { "type": "object", "format": "point" } },
          "properties": { "position": { "$ref": "#/$defs/Point" } }
        }
        """;

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Schema).GetFlattenedProperties().Single().DescribesGeospatialValue();

    [Fact] void should_describe_a_geospatial_value() => _result.ShouldBeTrue();
}
