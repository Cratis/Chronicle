// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_deserializing_formatted_values;

public class with_provider_specific_options : given.two_providers_with_formatted_value_options
{
    JsonObject _firstSource;
    JsonObject _secondSource;
    Point _firstResult;
    Point _secondResult;
    Point _firstResultAgain;

    void Establish()
    {
        _firstSource = JsonNode.Parse(
            """
            { "location": { "pointValue": { "type": "Point", "coordinates": [11.5, 12.5] } } }
            """).AsObject();
        _secondSource = JsonNode.Parse(
            """
            { "location": { "pointValue": { "type": "Point", "coordinates": [101.5, 102.5] } } }
            """).AsObject();
    }

    void Because()
    {
        dynamic first = firstConverter.ToExpandoObject(_firstSource, schema);
        dynamic second = secondConverter.ToExpandoObject(_secondSource, schema);
        dynamic firstAgain = firstConverter.ToExpandoObject(_firstSource, schema);
        _firstResult = (Point)first.location.pointValue;
        _secondResult = (Point)second.location.pointValue;
        _firstResultAgain = (Point)firstAgain.location.pointValue;
    }

    [Fact] void should_decode_with_first_provider_options() => _firstResult.ShouldEqual(new Point(1.5, 2.5));
    [Fact] void should_decode_with_second_provider_options() => _secondResult.ShouldEqual(new Point(1.5, 2.5));
    [Fact] void should_preserve_first_provider_decoding_after_using_second_provider() => _firstResultAgain.ShouldEqual(new Point(1.5, 2.5));
}
