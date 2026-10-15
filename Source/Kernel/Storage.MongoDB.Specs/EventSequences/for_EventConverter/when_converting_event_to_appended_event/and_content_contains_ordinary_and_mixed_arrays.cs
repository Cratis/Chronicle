// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_content_contains_ordinary_and_mixed_arrays : given.an_event_converter
{
    Event _event;
    AppendedEvent _result;

    void Establish()
    {
        _event = CreateEvent();
        _event.Content["1"] = BsonDocument.Parse("""
            {"strings":["a","b"],"integers":[1,2],"objects":[{"a":[1]}],
             "mixed":[1,{"$numberDecimal":"18446744073709551615"},"x",{"a":[1]}]}
            """);
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_preserve_strings() => ((object[])((IDictionary<string, object?>)_result.Content)["strings"]!)[0].ShouldEqual("a");
    [Fact] void should_preserve_small_integers() => ((object[])((IDictionary<string, object?>)_result.Content)["integers"]!)[1].ShouldEqual(2L);
    [Fact] void should_preserve_nested_objects() => ((object[])((IDictionary<string, object?>)((object[])((IDictionary<string, object?>)_result.Content)["objects"]!)[0])["a"]!)[0].ShouldEqual(1L);
    [Fact] void should_restore_the_large_unsigned_element() => ((object[])((IDictionary<string, object?>)_result.Content)["mixed"]!)[1].ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_the_mixed_array_in_generational_content() => JsonNode.DeepEquals(JsonNode.Parse(_result.GenerationalContent[1])!["mixed"], JsonNode.Parse("""[1,18446744073709551615,"x",{"a":[1]}]""")).ShouldBeTrue();
}
