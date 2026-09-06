// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_JsonPropertyPaths.when_resolving;

/// <summary>
/// Present-but-null is present. A default value fills in a property the payload does not carry, not one that
/// carries no value, so the two have to stay distinguishable.
/// </summary>
public class and_the_value_is_null : Specification
{
    JsonObject _root;
    bool _resolved;
    JsonNode _value;

    void Establish() => _root = new JsonObject { ["price"] = new JsonObject { ["description"] = null } };

    void Because() => _resolved = JsonPropertyPaths.TryResolve(_root, "price.description", out _value);

    [Fact] void should_resolve() => _resolved.ShouldBeTrue();
    [Fact] void should_return_null() => _value.ShouldBeNull();
}
