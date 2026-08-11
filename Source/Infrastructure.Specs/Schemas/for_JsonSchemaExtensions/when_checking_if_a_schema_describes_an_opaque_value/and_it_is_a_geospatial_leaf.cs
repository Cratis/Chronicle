// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_an_opaque_value;

/// <summary>
/// The shape the schema generator emits for a <c>Point</c>, <c>LineString</c> or <c>Polygon</c>: an object
/// carrying only its format, because the GeoJSON members on the wire belong to the type's own converter.
/// </summary>
public class and_it_is_a_geospatial_leaf : Specification
{
    const string Schema = """{"type":"object","format":"point","title":"Point"}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Schema).DescribesOpaqueValue();

    [Fact] void should_describe_an_opaque_value() => _result.ShouldBeTrue();
}
