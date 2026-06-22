// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_expando_with_geospatial_to_json_object : given.an_expando_object_converter_with_geospatial_schema
{
    ExpandoObject _source;
    JsonObject _result;

    void Establish()
    {
        _source = new ExpandoObject();
        var asDictionary = _source as IDictionary<string, object?>;
        asDictionary["pointValue"] = new Point(1.5, 2.5);
        asDictionary["lineStringValue"] = new LineString([new Point(1.0, 2.0), new Point(3.0, 4.0)]);
    }

    void Because() => _result = converter.ToJsonObject(_source, schema);

    [Fact] void should_write_point_as_geojson_type() => _result["pointValue"]["type"].GetValue<string>().ShouldEqual("Point");
    [Fact] void should_write_point_longitude() => _result["pointValue"]["coordinates"][0].GetValue<double>().ShouldEqual(1.5);
    [Fact] void should_write_point_latitude() => _result["pointValue"]["coordinates"][1].GetValue<double>().ShouldEqual(2.5);
    [Fact] void should_write_linestring_as_geojson_type() => _result["lineStringValue"]["type"].GetValue<string>().ShouldEqual("LineString");
    [Fact] void should_write_linestring_first_coordinate_longitude() => _result["lineStringValue"]["coordinates"][0][0].GetValue<double>().ShouldEqual(1.0);
}
