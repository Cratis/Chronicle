// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_round_tripping_a_date_time_value_with_an_offset : Specification
{
    const string WithOffset = "2025-01-02T03:04:05.1234567+05:30";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    EventEntry _entry;
    IDictionary<string, object?> _result;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"moment":{"type":"string","format":"date-time"}}}
            """);
        _entry = new EventEntry();
    }

    void Because()
    {
        var content = _converter.ToExpandoObject(Encoding.UTF8.GetBytes($$"""{"moment":"{{WithOffset}}"}"""), _schema);
        EventEntryConverter.UpdateContentForGeneration(_entry, 1, content);
        var stored = EventEntryConverter.GetContentForGeneration(_entry, 1);
        _result = _converter.ToExpandoObject(_converter.ToJsonObject(stored, _schema), _schema);
    }

    [Fact] void should_read_back_the_value_with_its_own_offset() => _result["moment"].ShouldEqual(DateTimeOffset.Parse(WithOffset));
    [Fact] void should_keep_the_offset() => ((DateTimeOffset)_result["moment"]!).Offset.ShouldEqual(TimeSpan.FromMinutes(330));
}
