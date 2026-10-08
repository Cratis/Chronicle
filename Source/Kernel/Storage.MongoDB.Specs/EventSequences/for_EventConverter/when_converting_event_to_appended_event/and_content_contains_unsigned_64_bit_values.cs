// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_content_contains_unsigned_64_bit_values : given.an_event_converter
{
    Event _event;
    AppendedEvent _result;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
                "value":{"type":"integer","format":"uint64"},
                "values":{"type":"array","items":{"type":"integer","format":"uint64"}},
                "small":{"type":"integer","format":"uint64"}
            }}
            """);
        var converter = new Json.ExpandoObjectConverter(new TypeFormats());
        _event = CreateEvent();
        _event.Content["1"] = new BsonDocument
        {
            ["value"] = new BsonDecimal128((decimal)ulong.MaxValue),
            ["values"] = new BsonArray { new BsonDecimal128((decimal)ulong.MaxValue) },
            ["small"] = new BsonInt64(long.MaxValue)
        };
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(true);
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(
            new EventTypeSchema(new EventType(_event.Type, 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(
            call => converter.ToExpandoObject(call.Arg<JsonObject>(), call.Arg<JsonSchema>()));
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_read_the_scalar_as_uint64() => ((IDictionary<string, object?>)_result.Content)["value"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_read_the_array_element_as_uint64() => ((object[])((IDictionary<string, object?>)_result.Content)["values"]!)[0].ShouldEqual(ulong.MaxValue);
    [Fact] void should_read_existing_int64_as_uint64() => ((IDictionary<string, object?>)_result.Content)["small"].ShouldEqual((ulong)long.MaxValue);
    [Fact] void should_expose_plain_json_for_the_generation() => JsonNode.Parse(_result.GenerationalContent[1])!["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
}
