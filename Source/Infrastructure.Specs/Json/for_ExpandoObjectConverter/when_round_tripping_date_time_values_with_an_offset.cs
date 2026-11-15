// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_date_time_values_with_an_offset : Specification
{
    const string OffsetValue = "2025-01-02T03:04:05.1234567+05:30";
    const string UtcValue = "2025-01-02T03:04:05.1234567Z";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    object _fromNode;
    object _fromUtf8;
    object _utcFromNode;
    string _offsetFromNode;
    string _offsetWritten;
    string _offsetSerialized;
    string _utcWritten;
    object _releasedFromNode;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"moment":{"type":"string","format":"date-time"}}}
            """);
    }

    void Because()
    {
        var offsetExpando = _converter.ToExpandoObject(Document(OffsetValue), _schema);
        _fromNode = Moment(offsetExpando);
        _offsetFromNode = ReadBack(offsetExpando);

        var utf8Expando = _converter.ToExpandoObject(Encoding.UTF8.GetBytes($$"""{"moment":"{{OffsetValue}}"}"""), _schema);
        _fromUtf8 = Moment(utf8Expando);
        _offsetSerialized = JsonSerializer.Serialize(utf8Expando);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            _converter.WriteJson(writer, utf8Expando, _schema);
        }

        _offsetWritten = JsonNode.Parse(stream.ToArray())!["moment"]!.GetValue<string>();

        // A written value must read back through the node path (compliance release deep-clones and re-reads it).
        _releasedFromNode = Moment(_converter.ToExpandoObject(_converter.ToJsonObject(utf8Expando, _schema), _schema));

        var utcExpando = _converter.ToExpandoObject(Document(UtcValue), _schema);
        _utcFromNode = Moment(utcExpando);
        _utcWritten = ReadBack(utcExpando);
    }

    [Fact] void should_hold_an_offset_value_from_a_node_as_a_date_time_offset() => _fromNode.ShouldEqual(DateTimeOffset.Parse(OffsetValue));
    [Fact] void should_keep_the_offset_of_a_value_from_a_node() => ((DateTimeOffset)_fromNode).Offset.ShouldEqual(TimeSpan.FromMinutes(330));
    [Fact] void should_hold_an_offset_value_from_utf8_as_a_date_time_offset() => ((DateTimeOffset)_fromUtf8).Offset.ShouldEqual(TimeSpan.FromMinutes(330));
    [Fact] void should_keep_the_offset_when_converting_to_a_json_object() => _offsetFromNode.ShouldEqual(OffsetValue);
    [Fact] void should_keep_the_offset_when_writing_json() => _offsetWritten.ShouldEqual(OffsetValue);
    [Fact] void should_keep_the_offset_when_serialized_with_plain_system_text_json() => _offsetSerialized.ShouldContain(OffsetValue);
    [Fact] void should_read_a_written_offset_value_back() => _releasedFromNode.ShouldEqual(_fromUtf8);
    [Fact] void should_hold_a_utc_value_as_a_date_time() => _utcFromNode.ShouldBeOfExactType<DateTime>();
    [Fact] void should_keep_a_utc_value_in_utc() => _utcWritten.ShouldEqual(UtcValue);

    static JsonObject Document(string value) => JsonNode.Parse($$"""{"moment":"{{value}}"}""")!.AsObject();

    static object Moment(ExpandoObject expando) => ((IDictionary<string, object?>)expando)["moment"]!;

    string ReadBack(ExpandoObject expando) => _converter.ToJsonObject(expando, _schema)["moment"]!.Deserialize<string>()!;
}
