// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_JsonPropertyPaths.when_setting;

public class and_the_path_addresses_the_elements_of_a_collection : Specification
{
    JsonObject _root;

    void Establish() => _root = new JsonObject
    {
        ["days"] = new JsonArray(
            new JsonObject { ["hours"] = 8 },
            new JsonObject { ["hours"] = 6, ["activity"] = "existing" })
    };

    void Because() => JsonPropertyPaths.SetWhereAbsent(_root, "days[].activity", JsonValue.Create("work"));

    [Fact] void should_write_the_value_into_the_element_without_one() => _root["days"]![0]!["activity"]!.GetValue<string>().ShouldEqual("work");
    [Fact] void should_keep_an_existing_value() => _root["days"]![1]!["activity"]!.GetValue<string>().ShouldEqual("existing");
    [Fact] void should_keep_every_element() => _root["days"]!.AsArray().Count.ShouldEqual(2);
}
