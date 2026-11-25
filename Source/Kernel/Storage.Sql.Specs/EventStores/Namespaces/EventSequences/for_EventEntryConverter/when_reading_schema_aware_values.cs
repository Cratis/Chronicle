// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_reading_schema_aware_values : given.a_schema_aware_read
{
    IDictionary<string, object?> _content;

    void Establish()
    {
        schema = JsonSchema.FromJson("""
            {
              "type":"object",
              "definitions":{"detail":{"type":"object","properties":{"count":{"type":"integer","format":"int32"}}}},
              "allOf":[{"type":"object","properties":{"inherited":{"type":"string"}}}],
              "properties":{
                "name":{"type":"string"}, "label":{"type":"string"},
                "missing":{"type":"integer","format":"int32"}, "empty":{"type":"string"},
                "amount":{"type":"number","format":"decimal"}, "maximum":{"type":"number","format":"decimal"},
                "wide":{"type":"integer","format":"uint64"}, "signed":{"type":"integer","format":"int64"},
                "moment":{"type":"string","format":"date-time-offset"}, "id":{"type":"string","format":"uuid"},
                "timestamp":{"type":"string","format":"date-time"}, "date":{"type":"string","format":"date"},
                "time":{"type":"string","format":"time"}, "optional":{"type":["integer","null"],"format":"int32?"},
                "detail":{"$ref":"#/definitions/detail"},
                "items":{"type":"array","items":{"$ref":"#/definitions/detail"}},
                "dictionary":{"type":"object","additionalProperties":{}},
                "unknown":{"type":"array"}
              }
            }
            """);
        entry.Content = """
            {"2":{
              "Name":"wrong", "name":"exact", "LABEL":"fallback", "empty":null,
              "amount":12345678901234567890.123456789, "maximum":79228162514264337593543950335,
              "wide":18446744073709551615, "signed":-9223372036854775808,
              "moment":"2025-01-02T03:04:05.1234567+05:30", "id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
              "timestamp":"2025-01-02T03:04:05.1234567Z", "date":"2025-01-02", "time":"03:04:05.1234567", "optional":null,
              "detail":{"COUNT":7}, "items":[{"count":9},null],
              "dictionary":{"Count":3,"empty":null,"nested":{"flag":true}},
              "unknown":[1,true,null,{"name":"nested"}], "inherited":"base", "extra":"ignored"
            }}
            """;
    }

    async Task Because()
    {
        result = await Read();
        _content = result.Content;
    }

    [Fact] void should_prefer_the_exact_property_name() => _content["name"].ShouldEqual("exact");
    [Fact] void should_match_other_property_names_case_insensitively() => _content["label"].ShouldEqual("fallback");
    [Fact] void should_supply_the_default_for_a_missing_value() => _content["missing"].ShouldEqual(0);
    [Fact] void should_omit_null_values() => _content.ContainsKey("empty").ShouldBeFalse();
    [Fact] void should_ignore_properties_outside_the_schema() => _content.ContainsKey("extra").ShouldBeFalse();
    [Fact] void should_preserve_decimal_precision() => _content["amount"].ShouldEqual(12345678901234567890.123456789m);
    [Fact] void should_preserve_the_maximum_decimal() => _content["maximum"].ShouldEqual(decimal.MaxValue);
    [Fact] void should_preserve_unsigned_integer_precision() => _content["wide"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_signed_integer_precision() => _content["signed"].ShouldEqual(long.MinValue);
    [Fact] void should_omit_nullable_primitive_nulls() => _content.ContainsKey("optional").ShouldBeFalse();
    [Fact] void should_restore_the_date_time() => _content["timestamp"].ShouldEqual(DateTime.Parse("2025-01-02T03:04:05.1234567Z").ToUniversalTime());
    [Fact] void should_restore_the_date() => _content["date"].ShouldEqual(new DateOnly(2025, 1, 2));
    [Fact] void should_restore_the_time() => _content["time"].ShouldEqual(TimeOnly.Parse("03:04:05.1234567"));
    [Fact] void should_restore_the_formatted_identifier() => _content["id"].ShouldEqual(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    [Fact] void should_restore_the_formatted_timestamp() => _content["moment"].ShouldEqual(DateTimeOffset.Parse("2025-01-02T03:04:05.1234567+05:30"));
    [Fact] void should_preserve_the_timestamp_offset() => ((DateTimeOffset)_content["moment"]!).Offset.ShouldEqual(TimeSpan.FromMinutes(330));
    [Fact] void should_follow_nested_schema_references() => ((IDictionary<string, object?>)_content["detail"]!)["count"].ShouldEqual(7);
    [Fact] void should_follow_array_item_references() => ((IDictionary<string, object?>)((object?[])_content["items"]!)[0]!)["count"].ShouldEqual(9);
    [Fact] void should_keep_null_array_items() => ((object?[])_content["items"]!)[1].ShouldBeNull();
    [Fact] void should_read_inherited_schema_properties() => _content["inherited"].ShouldEqual("base");
    [Fact] void should_preserve_dictionary_key_casing() => ((IDictionary<string, object?>)_content["dictionary"]!)["Count"].ShouldEqual(3d);
    [Fact] void should_preserve_null_dictionary_values() => ((IDictionary<string, object?>)_content["dictionary"]!)["empty"].ShouldBeNull();
    [Fact] void should_preserve_nested_dictionary_values() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_content["dictionary"]!)["nested"]!)["flag"].ShouldEqual(true);
    [Fact] void should_read_unknown_numeric_array_items_as_doubles() => ((object?[])_content["unknown"]!)[0].ShouldEqual(1d);
    [Fact] void should_preserve_unknown_boolean_array_items() => ((object?[])_content["unknown"]!)[1].ShouldEqual(true);
    [Fact] void should_preserve_unknown_null_array_items() => ((object?[])_content["unknown"]!)[2].ShouldBeNull();
    [Fact] void should_preserve_unknown_object_array_items() => ((IDictionary<string, object?>)((object?[])_content["unknown"]!)[3]!)["name"].ShouldEqual("nested");
}
