// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_DateTimeValues;

public class when_holding_content_as_before_offsets_were_kept : Specification
{
    const string WithOffset = "2025-01-02T03:04:05.1234567+05:30";

    JsonSchema _schema;
    ExpandoObject _content;
    ExpandoObject _result;
    IDictionary<string, object?> _values;
    IDictionary<string, object?> _nested;
    object?[] _items;

    void Establish()
    {
        var converter = new ExpandoObjectConverter(new TypeFormats());
        _schema = Schema();
        _content = converter.ToExpandoObject(
            Encoding.UTF8.GetBytes($$"""
                {
                    "moment": "{{WithOffset}}",
                    "inUtc": "2025-01-01T21:34:05Z",
                    "withOffsetFormat": "{{WithOffset}}",
                    "nested": { "moment": "{{WithOffset}}" },
                    "moments": ["{{WithOffset}}"]
                }
                """),
            _schema);
    }

    void Because()
    {
        _result = DateTimeValues.AsHeldBeforeOffsetsWereKept(_content, _schema);
        _values = _result;
        _nested = (ExpandoObject)_values["nested"]!;
        _items = (object?[])_values["moments"]!;
    }

    [Fact] void should_hold_a_date_time_value_in_the_local_time_of_the_host() => _values["moment"].ShouldEqual(DateTimeOffset.Parse(WithOffset).LocalDateTime);
    [Fact] void should_hold_a_date_time_value_as_a_local_date_time() => ((DateTime)_values["moment"]!).Kind.ShouldEqual(DateTimeKind.Local);
    [Fact] void should_leave_a_utc_date_time_value_as_it_is() => _values["inUtc"].ShouldEqual(((IDictionary<string, object?>)_content)["inUtc"]);
    [Fact] void should_leave_a_date_time_offset_value_as_it_is() => _values["withOffsetFormat"].ShouldEqual(DateTimeOffset.Parse(WithOffset));
    [Fact] void should_hold_a_nested_date_time_value_in_the_local_time_of_the_host() => _nested["moment"].ShouldEqual(DateTimeOffset.Parse(WithOffset).LocalDateTime);
    [Fact] void should_hold_date_time_values_in_a_collection_in_the_local_time_of_the_host() => _items[0].ShouldEqual(DateTimeOffset.Parse(WithOffset).LocalDateTime);
    [Fact] void should_not_change_the_content_given() => ((IDictionary<string, object?>)_content)["moment"].ShouldEqual(DateTimeOffset.Parse(WithOffset));

    static JsonSchema Schema() => JsonSchema.FromJson("""
        {
            "type": "object",
            "properties": {
                "moment": { "type": "string", "format": "date-time" },
                "inUtc": { "type": "string", "format": "date-time" },
                "withOffsetFormat": { "type": "string", "format": "date-time-offset" },
                "nested": { "type": "object", "properties": { "moment": { "type": "string", "format": "date-time" } } },
                "moments": { "type": "array", "items": { "type": "string", "format": "date-time" } }
            }
        }
        """);
}
