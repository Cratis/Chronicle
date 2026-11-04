// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_JsonPropertyPaths.when_setting;

public class and_the_path_goes_through_a_collection_without_addressing_its_elements : Specification
{
    JsonObject _root;
    Exception _error;

    void Establish() => _root = new JsonObject
    {
        ["days"] = new JsonArray(new JsonObject { ["hours"] = 8 })
    };

    void Because() => _error = Catch.Exception(() => JsonPropertyPaths.Set(_root, "days.activity", JsonValue.Create("work")));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<CannotWritePropertyPathThroughCollection>();
    [Fact] void should_keep_the_collection() => _root["days"]!.AsArray().Count.ShouldEqual(1);
}
