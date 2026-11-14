// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Events.for_AppendedEventConverters;

public class when_converting_an_event_with_a_date_time_value_with_an_offset_to_a_contract : Specification
{
    const string WithOffset = "2025-01-02T03:04:05.1234567+05:30";

    AppendedEvent _event;
    Contracts.Events.AppendedEvent _result;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"moment":{"type":"string","format":"date-time"}}}
            """);
        var content = new ExpandoObjectConverter(new TypeFormats())
            .ToExpandoObject(Encoding.UTF8.GetBytes($$"""{"moment":"{{WithOffset}}"}"""), schema);
        _event = new AppendedEvent(
            EventContext.From("store", "default", new EventType("moment-set", 1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, CorrelationId.NotSet),
            content);
    }

    void Because() => _result = _event.ToContract(new JsonSerializerOptions());

    [Fact] void should_deliver_the_value_with_its_own_offset() =>
        JsonElement.Parse(_result.Content).GetProperty("moment").GetString().ShouldEqual(WithOffset);
}
