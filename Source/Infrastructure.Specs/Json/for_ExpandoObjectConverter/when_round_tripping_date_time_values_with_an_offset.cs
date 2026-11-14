// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_date_time_values_with_an_offset : Specification
{
    const string OffsetValue = "2025-01-02T03:04:05.1234567+05:30";
    const string UtcValue = "2025-01-02T03:04:05.1234567Z";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    string _offsetFromNode;
    string _offsetFromUtf8;
    string _offsetWritten;
    string _utcFromNode;
    DateTime _utcInstantFromNode;
    DateTime _offsetInstantFromNode;

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
        _offsetFromNode = ReadBack(offsetExpando);
        _offsetInstantFromNode = ((DateTime)((IDictionary<string, object?>)offsetExpando)["moment"]!).ToUniversalTime();

        var utf8Expando = _converter.ToExpandoObject(Encoding.UTF8.GetBytes($$"""{"moment":"{{OffsetValue}}"}"""), _schema);
        _offsetFromUtf8 = ReadBack(utf8Expando);

        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            _converter.WriteJson(writer, utf8Expando, _schema);
        }

        _offsetWritten = Encoding.UTF8.GetString(stream.ToArray());

        var utcExpando = _converter.ToExpandoObject(Document(UtcValue), _schema);
        _utcFromNode = ReadBack(utcExpando);
        _utcInstantFromNode = (DateTime)((IDictionary<string, object?>)utcExpando)["moment"]!;
    }

    [Fact] void should_keep_the_offset_when_converting_from_a_node() => _offsetFromNode.ShouldEqual(OffsetValue);
    [Fact] void should_keep_the_offset_when_converting_from_utf8() => _offsetFromUtf8.ShouldEqual(OffsetValue);
    [Fact] void should_keep_the_offset_when_writing_json() => _offsetWritten.ShouldContain(OffsetValue);
    [Fact] void should_expose_the_instant_of_an_offset_value() => _offsetInstantFromNode.ShouldEqual(new DateTime(2025, 1, 1, 21, 34, 5, DateTimeKind.Utc).AddTicks(1234567));
    [Fact] void should_expose_the_instant_of_a_utc_value() => _utcInstantFromNode.ToUniversalTime().ShouldEqual(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567));
    [Fact] void should_keep_a_utc_value_in_utc() => _utcFromNode.ShouldEqual(UtcValue);

    static JsonObject Document(string value) => JsonNode.Parse($$"""{"moment":"{{value}}"}""")!.AsObject();

    string ReadBack(ExpandoObject expando) => _converter.ToJsonObject(expando, _schema)["moment"]!.ToJsonString().Trim('"');
}
