// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_JsonPropertyPaths.when_setting;

/// <summary>
/// A migration adding a value inside a nested object is written into a payload that may not carry that object yet,
/// so the objects along the way are created rather than treated as an error.
/// </summary>
public class and_the_nested_object_does_not_exist : Specification
{
    JsonObject _root;

    void Establish() => _root = [];

    void Because() => JsonPropertyPaths.Set(_root, "price.description", JsonValue.Create("unspecified"));

    [Fact] void should_create_the_nested_object() => _root["price"].ShouldBeOfExactType<JsonObject>();
    [Fact] void should_write_the_value_inside_it() => _root["price"]!["description"]!.GetValue<string>().ShouldEqual("unspecified");
    [Fact] void should_not_create_a_dotted_top_level_key() => _root.ContainsKey("price.description").ShouldBeFalse();
}
