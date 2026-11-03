// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_serializing_formatted_values;

public class with_provider_specific_options : given.two_providers_with_formatted_value_options
{
    ExpandoObject _source;
    JsonObject _firstResult;
    JsonObject _secondResult;
    JsonObject _firstResultAgain;

    void Establish()
    {
        dynamic location = new ExpandoObject();
        location.pointValue = new Point(1.5, 2.5);
        dynamic source = new ExpandoObject();
        source.location = location;
        _source = source;
    }

    void Because()
    {
        _firstResult = firstConverter.ToJsonObject(_source, schema);
        _secondResult = secondConverter.ToJsonObject(_source, schema);
        _firstResultAgain = firstConverter.ToJsonObject(_source, schema);
    }

    [Fact] void should_use_first_provider_longitude_encoding() => _firstResult["location"]["pointValue"]["coordinates"][0].GetValue<double>().ShouldEqual(11.5);
    [Fact] void should_use_first_provider_latitude_encoding() => _firstResult["location"]["pointValue"]["coordinates"][1].GetValue<double>().ShouldEqual(12.5);
    [Fact] void should_use_second_provider_longitude_encoding() => _secondResult["location"]["pointValue"]["coordinates"][0].GetValue<double>().ShouldEqual(101.5);
    [Fact] void should_use_second_provider_latitude_encoding() => _secondResult["location"]["pointValue"]["coordinates"][1].GetValue<double>().ShouldEqual(102.5);
    [Fact] void should_preserve_first_provider_encoding_after_using_second_provider() => _firstResultAgain.ToJsonString().ShouldEqual(_firstResult.ToJsonString());
}
