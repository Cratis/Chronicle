// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_type_with_geospatial_properties : given.a_json_schema_generator
{
    record TypeWithGeospatial(Point APoint, LineString ALine, Polygon APolygon);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(TypeWithGeospatial));

    [Fact] void should_inject_point_format_for_point_property() => _result.ActualProperties["APoint"].Format.ShouldEqual("point");
    [Fact] void should_inject_linestring_format_for_linestring_property() => _result.ActualProperties["ALine"].Format.ShouldEqual("linestring");
    [Fact] void should_inject_polygon_format_for_polygon_property() => _result.ActualProperties["APolygon"].Format.ShouldEqual("polygon");
    [Fact] void should_emit_point_as_leaf_without_sub_properties() => _result.ActualProperties["APoint"].ActualSchema.ActualProperties.ShouldBeEmpty();
    [Fact] void should_emit_linestring_as_leaf_without_sub_properties() => _result.ActualProperties["ALine"].ActualSchema.ActualProperties.ShouldBeEmpty();
    [Fact] void should_emit_polygon_as_leaf_without_sub_properties() => _result.ActualProperties["APolygon"].ActualSchema.ActualProperties.ShouldBeEmpty();
}
