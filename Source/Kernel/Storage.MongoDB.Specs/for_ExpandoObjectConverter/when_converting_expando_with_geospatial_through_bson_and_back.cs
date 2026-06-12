// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_expando_with_geospatial_through_bson_and_back : given.an_expando_object_converter_with_geospatial_schema
{
    Point _point;
    LineString _lineString;
    Polygon _polygon;
    dynamic _result;

    void Establish()
    {
        _point = new Point(59.752077, -33.260638);
        _lineString = new LineString([new Point(42.123, 10.456), new Point(43.456, 11.789)]);
        _polygon = new Polygon(new LinearRing([new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 0)]), []);

        var source = new ExpandoObject() as IDictionary<string, object?>;
        source["pointValue"] = _point;
        source["lineStringValue"] = _lineString;
        source["polygonValue"] = _polygon;

        var document = converter.ToBsonDocument((ExpandoObject)source, schema);
        _result = converter.ToExpandoObject(document, schema);
    }

    [Fact] void should_round_trip_point_as_typed_value() => ((object)_result.pointValue).ShouldBeOfExactType<Point>();
    [Fact] void should_preserve_point_value() => ((Point)_result.pointValue).ShouldEqual(_point);
    [Fact] void should_round_trip_line_string_as_typed_value() => ((object)_result.lineStringValue).ShouldBeOfExactType<LineString>();
    [Fact] void should_preserve_line_string_coordinates() => ((LineString)_result.lineStringValue).Coordinates.ShouldContainOnly(_lineString.Coordinates);
    [Fact] void should_round_trip_polygon_as_typed_value() => ((object)_result.polygonValue).ShouldBeOfExactType<Polygon>();
    [Fact] void should_preserve_polygon_shell_coordinates() => ((Polygon)_result.polygonValue).Shell.Coordinates.ShouldContainOnly(_polygon.Shell.Coordinates);
}
