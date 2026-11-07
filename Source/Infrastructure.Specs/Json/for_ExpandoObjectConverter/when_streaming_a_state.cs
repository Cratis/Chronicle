// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

/// <summary>
/// Writing a state as it is walked and reading it as it is parsed must give exactly what going through a tree of nodes gives.
/// </summary>
public class when_streaming_a_state : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _state;
    string _viaNodes;
    string _viaWriter;
    string _readViaReader;
    string _readViaNodes;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromType<StreamedContainer>();

        var entries = Enumerable.Range(0, 25).Select(i => new JsonObject
        {
            ["id"] = Guid.NewGuid(),
            ["name"] = $"entry \"{i}\" æøå",
            ["count"] = i,
            ["created"] = new DateTime(2026, 1, 1, 12, 0, i, DateTimeKind.Utc),
            ["note"] = i % 3 == 0 ? null : $"note {i}",
            ["tags"] = new JsonArray("a", "b"),
            ["labels"] = new JsonObject { ["x"] = "1", ["y"] = "2" }
        }).ToArray();

        var source = new JsonObject
        {
            ["name"] = "container",
            ["limit"] = null,
            ["primary"] = entries[0].DeepClone(),
            ["entries"] = new JsonArray([.. entries.Skip(1).Select(_ => _.DeepClone())]),
            ["aliases"] = new JsonArray("one", "two"),
            ["matrix"] = new JsonArray(new JsonArray(1, 2), new JsonArray(3))
        };

        _state = _converter.ToExpandoObject(source, _schema);
    }

    void Because()
    {
        var options = new JsonSerializerOptions();
        _viaNodes = _converter.ToJsonObject(_state, _schema).ToJsonString(options);
        _viaWriter = LargeJson.Serialize(writer => _converter.WriteJson(writer, _state, _schema), options);

        var bytes = LargeJson.ToUtf8(_viaNodes);
        _readViaReader = _converter.ToJsonObject(_converter.ToExpandoObject(bytes, _schema), _schema).ToJsonString(options);
        _readViaNodes = _converter.ToJsonObject(_converter.ToExpandoObject(JsonNode.Parse(_viaNodes)!.AsObject(), _schema), _schema).ToJsonString(options);
    }

    [Fact] void should_write_the_same_json_as_building_the_nodes() => _viaWriter.ShouldEqual(_viaNodes);
    [Fact] void should_write_entries() => JsonNode.Parse(_viaWriter)!["entries"]!.AsArray().Count.ShouldEqual(24);
    [Fact] void should_read_the_same_state_as_parsing_the_nodes() => _readViaReader.ShouldEqual(_readViaNodes);
    [Fact] void should_read_back_what_was_written() => _readViaReader.ShouldEqual(_viaNodes);
}
