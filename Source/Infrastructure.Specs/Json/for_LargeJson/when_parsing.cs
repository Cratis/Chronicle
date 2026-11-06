// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_LargeJson;

public class when_parsing : Specification
{
    JsonNode? _result;

    void Because() => _result = LargeJson.Parse("""{"name":"æøå 😀","list":[1,2,3],"nested":{"flag":true}}""");

    [Fact] void should_read_a_string_with_multi_byte_characters() => _result!["name"]!.GetValue<string>().ShouldEqual("æøå 😀");
    [Fact] void should_read_an_array() => _result!["list"]!.AsArray().Count.ShouldEqual(3);
    [Fact] void should_read_a_nested_object() => _result!["nested"]!["flag"]!.GetValue<bool>().ShouldBeTrue();
}
