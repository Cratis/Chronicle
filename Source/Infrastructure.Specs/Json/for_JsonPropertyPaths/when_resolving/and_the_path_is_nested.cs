// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_JsonPropertyPaths.when_resolving;

public class and_the_path_is_nested : Specification
{
    JsonObject _root;
    bool _resolved;
    JsonNode _value;

    void Establish() => _root = new JsonObject
    {
        ["price"] = new JsonObject { ["amount"] = 42, ["description"] = "a thing" }
    };

    void Because() => _resolved = JsonPropertyPaths.TryResolve(_root, "price.description", out _value);

    [Fact] void should_resolve() => _resolved.ShouldBeTrue();
    [Fact] void should_return_the_nested_value() => _value.GetValue<string>().ShouldEqual("a thing");
}
