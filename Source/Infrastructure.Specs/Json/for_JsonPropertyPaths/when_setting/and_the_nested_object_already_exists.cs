// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_JsonPropertyPaths.when_setting;

public class and_the_nested_object_already_exists : Specification
{
    JsonObject _root;

    void Establish() => _root = new JsonObject
    {
        ["price"] = new JsonObject { ["amount"] = 42 }
    };

    void Because() => JsonPropertyPaths.Set(_root, "price.description", JsonValue.Create("a thing"));

    [Fact] void should_write_the_value() => _root["price"]!["description"]!.GetValue<string>().ShouldEqual("a thing");
    [Fact] void should_keep_the_siblings() => _root["price"]!["amount"]!.GetValue<int>().ShouldEqual(42);
}
