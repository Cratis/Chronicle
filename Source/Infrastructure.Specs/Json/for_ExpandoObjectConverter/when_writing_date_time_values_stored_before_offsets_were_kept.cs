// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_writing_date_time_values_stored_before_offsets_were_kept : Specification
{
    const string StoredWithHostOffset = "2025-01-01T22:34:05.1234567+01:00";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    string _storedWithOffset;
    string _storedWithOffsetSerialized;
    string _storedWithoutOffset;
    string _computedUtc;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"moment":{"type":"string","format":"date-time"}}}
            """);
    }

    void Because()
    {
        var stored = Read(StoredWithHostOffset);
        _storedWithOffset = Written(stored);
        _storedWithOffsetSerialized = JsonSerializer.Serialize(stored);
        _storedWithoutOffset = Written(Read("2025-01-01T21:34:05.1234567"));

        dynamic computed = new ExpandoObject();
        computed.moment = new DateTime(2025, 1, 1, 21, 34, 5, DateTimeKind.Utc).AddTicks(1234567);
        _computedUtc = Written((ExpandoObject)computed);
    }

    [Fact] void should_write_a_stored_value_with_its_stored_offset() => _storedWithOffset.ShouldEqual(StoredWithHostOffset);
    [Fact] void should_serialize_a_stored_value_with_its_stored_offset() => _storedWithOffsetSerialized.ShouldEqual($$"""{"moment":"{{StoredWithHostOffset}}"}""");
    [Fact] void should_write_a_stored_value_without_an_offset_as_it_was_stored() => _storedWithoutOffset.ShouldEqual("2025-01-01T21:34:05.1234567");
    [Fact] void should_write_a_value_computed_in_utc_as_utc() => _computedUtc.ShouldEqual("2025-01-01T21:34:05.1234567Z");

    ExpandoObject Read(string value) => _converter.ToExpandoObject(Encoding.UTF8.GetBytes($$"""{"moment":"{{value}}"}"""), _schema);

    string Written(ExpandoObject expando) => _converter.ToJsonObject(expando, _schema)["moment"]!.Deserialize<string>()!;
}
