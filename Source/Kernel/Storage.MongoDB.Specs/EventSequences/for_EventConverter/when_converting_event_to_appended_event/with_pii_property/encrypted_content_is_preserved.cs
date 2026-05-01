// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event.with_pii_property;

public class encrypted_content_is_preserved : given.an_event_converter
{
    const string EncryptedValue = "encrypted::abc123";
    Event _event;
    AppendedEvent _result;
    EventTypeSchema _schema;
    ExpandoObject _convertedContent;

    void Establish()
    {
        _schema = new EventTypeSchema(
            new EventType(Guid.NewGuid().ToString(), 1),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            new JsonSchema());

        _convertedContent = new ExpandoObject();
        ((IDictionary<string, object?>)_convertedContent)["name"] = EncryptedValue;

        _event = CreateEvent();
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(true);
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(_schema);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(_convertedContent);
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_use_schema_based_converter() => _expandoObjectConverter.Received(1).ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>());
    [Fact] void should_return_the_raw_converted_content() => ((IDictionary<string, object?>)_result.Content)["name"].ShouldEqual(EncryptedValue);
}
