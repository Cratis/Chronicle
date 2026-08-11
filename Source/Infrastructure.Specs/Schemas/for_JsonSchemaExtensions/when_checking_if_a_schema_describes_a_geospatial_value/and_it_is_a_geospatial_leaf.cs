// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_a_geospatial_value;

/// <summary>
/// The shape the schema generator emits for a <c>Point</c>, <c>LineString</c> or <c>Polygon</c>: an object
/// carrying only its format, because the GeoJSON members on the wire belong to the type's own converter.
/// </summary>
public class and_it_is_a_geospatial_leaf : Specification
{
    const string Point = """{"type":"object","format":"point","title":"Point"}""";
    const string LineString = """{"type":"object","format":"linestring","title":"LineString"}""";
    const string Polygon = """{"type":"object","format":"polygon","title":"Polygon"}""";

    [Fact] void should_describe_a_geospatial_value_for_a_point() => JsonSchema.FromJson(Point).DescribesGeospatialValue().ShouldBeTrue();
    [Fact] void should_describe_a_geospatial_value_for_a_line_string() => JsonSchema.FromJson(LineString).DescribesGeospatialValue().ShouldBeTrue();
    [Fact] void should_describe_a_geospatial_value_for_a_polygon() => JsonSchema.FromJson(Polygon).DescribesGeospatialValue().ShouldBeTrue();
}
