// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_geojson_to_expando_object : given.an_expando_object_converter_with_geospatial_schema
{
    JsonObject _source;
    dynamic _result;

    void Establish() =>
        _source = new JsonObject
        {
            ["pointValue"] = new JsonObject
            {
                ["type"] = "Point",
                ["coordinates"] = new JsonArray(1.5, 2.5)
            },
            ["lineStringValue"] = new JsonObject
            {
                ["type"] = "LineString",
                ["coordinates"] = new JsonArray(
                    new JsonArray(1.0, 2.0),
                    new JsonArray(3.0, 4.0))
            }
        };

    void Because() => _result = converter.ToExpandoObject(_source, schema);

    [Fact] void should_materialize_point_as_typed_point() => ((object)_result.pointValue).ShouldBeOfExactType<Point>();
    [Fact] void should_set_point_longitude() => ((Point)_result.pointValue).Longitude.ShouldEqual(1.5);
    [Fact] void should_set_point_latitude() => ((Point)_result.pointValue).Latitude.ShouldEqual(2.5);
    [Fact] void should_materialize_linestring_as_typed_linestring() => ((object)_result.lineStringValue).ShouldBeOfExactType<LineString>();
    [Fact] void should_set_linestring_first_coordinate() => ((LineString)_result.lineStringValue).Coordinates[0].ShouldEqual(new Point(1.0, 2.0));
    [Fact] void should_set_linestring_second_coordinate() => ((LineString)_result.lineStringValue).Coordinates[1].ShouldEqual(new Point(3.0, 4.0));
}
