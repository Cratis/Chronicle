// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class content_with_storage_conversions : a_stored_event
{
    protected JsonSchema _schema;
    protected ExpandoObject _content;
    protected Dictionary<EventTypeGeneration, ExpandoObject> _generations;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            { "type": "object", "properties": {
                "status": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Pending", "Complete"] },
                "date": { "type": "string", "format": "date" },
                "timestamp": { "type": "string", "format": "date-time" },
                "offset": { "type": "string", "format": "date-time-offset" },
                "amount": { "type": "number", "format": "decimal" },
                "nested": { "type": "object", "properties": { "amount": { "type": "number", "format": "decimal" } } },
                "items": { "type": "array", "items": { "type": "object", "properties": { "status": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Pending", "Complete"] } } } },
                "numbers": { "type": "array", "items": { "type": "number", "format": "decimal" } }
            } }
            """);
        _command = _command with
        {
            Content = """
                { "status": 1, "date": "2026-03-04", "timestamp": "2026-03-04T05:06:07.1234567Z",
                  "offset": "2026-03-04T05:06:07.1234567+02:30", "amount": 123.12345678901234567890123456,
                  "nested": { "amount": 0.1234567890123456789012345678, "providerExtra": "discarded" },
                  "items": [{ "status": 1 }, { "status": 0 }], "numbers": [1.234567890123456789, 9.876543210987654321],
                  "providerExtra": "discarded" }
                """
        };
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, _schema));
        _content = _converter.ToExpandoObject(JsonNode.Parse(_command.Content)!.AsObject(), _schema);
        _generations = new() { [EventTypeGeneration.First] = _content };
    }
}
