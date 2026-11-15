// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_values_have_schema_formats : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-2b-component-key");
    JsonObject _content;
    JsonSchema _schema;
    UniqueConstraintEventDefinition _definition;
    IReadOnlyList<UniqueConstraintComponent> _components;
    string _encoded;
    string _expected;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
                "identifier":{"type":"string","format":"uuid"},"literal":{"type":"string"},
                "quantity":{"type":"integer","format":"int64"},"price":{"type":"number","format":"decimal"},
                "enabled":{"type":"boolean"},"day":{"type":"string","format":"date"},
                "at":{"type":"string","format":"time"},"elapsed":{"type":"string","format":"duration"},
                "instant":{"type":"string","format":"date-time-offset"},
                "utc":{"type":"string","format":"date-time"},
                "identifiers":{"type":"array","items":{"type":"string","format":"guid"}},
                "days":{"type":"array","items":{"type":"string","format":"date"}}}}
            """);
        _content = JsonNode.Parse("""
            {"identifier":"{ABCDEF01-2345-6789-ABCD-EF0123456789}","literal":"{ABCDEF01-2345-6789-ABCD-EF0123456789}",
             "quantity":9223372036854775806,"price":12.3400,"enabled":true,"day":"2025-02-03",
             "at":"04:05:06.1234567","elapsed":"01:02:03","instant":"2025-02-03T06:05:06+02:00",
             "utc":"2025-02-03T04:05:06Z","identifiers":[null,"ABCDEF01-2345-6789-ABCD-EF0123456789"],"days":["2025-02-03",null],
             "outside":"{ABCDEF01-2345-6789-ABCD-EF0123456789}"}
            """)!.AsObject();
        _definition = new("event", ["identifier", "literal", "quantity", "price", "enabled", "day", "at", "elapsed", "instant", "utc", "identifiers", "days", "outside"]);
    }

    void Because()
    {
        _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(_definition, _content, _schema);
        _encoded = UniqueConstraintEncoder.Encode(_key, _components).AsT0.Value;
        var guid = new Guid("abcdef01-2345-6789-abcd-ef0123456789");
        _expected = UniqueConstraintEncoder.Encode(_key,
        [
            new(guid, "uuid"), new("{ABCDEF01-2345-6789-ABCD-EF0123456789}"),
            new(9223372036854775806L, "int64"), new(12.3400m, "decimal"), new(true),
            new(new DateOnly(2025, 2, 3), "date"), new(TimeOnly.Parse("04:05:06.1234567", CultureInfo.InvariantCulture), "time"),
            new(new TimeSpan(1, 2, 3), "duration"),
            new(new DateTimeOffset(2025, 2, 3, 6, 5, 6, TimeSpan.FromHours(2)), "date-time-offset"),
            new(new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc), "date-time"),
            new(new Guid?[] { null, guid }, "guid"), new(new DateOnly?[] { new(2025, 2, 3), null }, "date"),
            new("{ABCDEF01-2345-6789-ABCD-EF0123456789}", IsInSchema: false)
        ]).AsT0.Value;
    }

    [Fact] void should_encode_the_same_as_typed_clr_components() => _encoded.ShouldEqual(_expected);
    [Fact] void should_leave_declared_formatless_guid_text_unchanged() => _components[1].Value.ShouldEqual("{ABCDEF01-2345-6789-ABCD-EF0123456789}");
    [Fact] void should_mark_declared_formatless_text_as_in_schema() => _components[1].IsInSchema.ShouldBeTrue();
    [Fact] void should_only_enable_guid_string_fallback_outside_schema() => _components[12].IsInSchema.ShouldBeFalse();
    [Fact] void should_keep_the_array_element_format() => _components[10].Format.ShouldEqual("guid");
    [Fact] void should_keep_array_null_positions() => ((object?[])_components[10].Value!)[0].ShouldBeNull();
}
