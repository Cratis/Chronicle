// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_LargeJson;

public class when_serializing : Specification
{
    JsonObject _node;
    string _withoutHint;
    string _withTooSmallHint;
    string _withExactHint;

    void Establish()
    {
        var items = new JsonArray();
        for (var i = 0; i < 2000; i++)
        {
            items.Add(new JsonObject { ["name"] = $"item \"{i}\" æøå", ["value"] = i });
        }

        _node = new JsonObject { ["items"] = items };
    }

    void Because()
    {
        var options = new JsonSerializerOptions();
        _withoutHint = LargeJson.Serialize(_node, options);
        _withTooSmallHint = LargeJson.Serialize(_node, options, 10);
        _withExactHint = LargeJson.Serialize(_node, options, _withoutHint.Length);
    }

    [Fact] void should_write_the_same_json_as_the_framework() => _withoutHint.ShouldEqual(_node.ToJsonString(new JsonSerializerOptions()));
    [Fact] void should_write_compact_json() => _withoutHint.ShouldNotContain("\n");
    [Fact] void should_write_the_same_json_when_the_hint_is_too_small() => _withTooSmallHint.ShouldEqual(_withoutHint);
    [Fact] void should_write_the_same_json_when_the_hint_is_exact() => _withExactHint.ShouldEqual(_withoutHint);
}
