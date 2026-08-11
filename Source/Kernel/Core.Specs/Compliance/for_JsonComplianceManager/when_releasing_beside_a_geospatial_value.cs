// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

/// <summary>
/// Release has to stop at the same values apply stopped at. An asymmetry here does not surface as a missing
/// value on write - it takes the whole query down on read.
/// </summary>
public class when_releasing_beside_a_geospatial_value : given.a_value_handler_and_a_type_with_a_compliant_member_beside_a_geospatial_value
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";

    JsonObject _result;
    Exception _exception;

    void Establish()
    {
        _input["organizerDisplayName"] = "encrypted";
        _valueHandler.Release(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create(DisplayName)));
    }

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _manager.Release(string.Empty, string.Empty, _schema, Identifier, _input));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_release_the_compliant_member() => _result["organizerDisplayName"]!.GetValue<string>().ShouldEqual(DisplayName);
    [Fact] void should_keep_the_geospatial_type() => _result["location"]!["locationPoint"]!["type"]!.GetValue<string>().ShouldEqual("Point");
    [Fact] void should_keep_the_geospatial_longitude() => _result["location"]!["locationPoint"]!["coordinates"]![0]!.GetValue<double>().ShouldEqual(Longitude);
    [Fact] void should_keep_the_geospatial_latitude() => _result["location"]!["locationPoint"]!["coordinates"]![1]!.GetValue<double>().ShouldEqual(Latitude);
    [Fact] void should_keep_the_non_compliant_sibling() => _result["location"]!["city"]!.GetValue<string>().ShouldEqual(City);
}
