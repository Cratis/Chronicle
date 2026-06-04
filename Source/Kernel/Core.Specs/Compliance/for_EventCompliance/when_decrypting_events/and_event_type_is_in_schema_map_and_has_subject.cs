// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Compliance.for_EventCompliance.when_decrypting_events;

public class and_event_type_is_in_schema_map_and_has_subject : given.all_dependencies
{
    AppendedEvent _event;
    AppendedEvent[] _result;

    void Establish()
    {
        dynamic content = new ExpandoObject();
        content.name = "original-name";
        _event = new AppendedEvent(
            EventContext.Empty with { EventType = SomeEventType, Subject = new Subject(SubjectValue) },
            content);
    }

    async Task Because() => _result = await _compliance.DecryptEvents(
        [_event],
        new Dictionary<EventType, EventTypeSchema> { { SomeEventType, new EventTypeSchema(SomeEventType, EventTypeOwner.Client, EventTypeSource.Code, _schemaWithPii) } });

    [Fact] void should_call_compliance_manager_release() =>
        _complianceManager.Received(1).Release(
            _event.Context.EventStore,
            _event.Context.Namespace,
            _schemaWithPii,
            SubjectValue,
            Arg.Any<JsonObject>());

    [Fact] void should_return_event_with_decrypted_content() =>
        ((IDictionary<string, object?>)_result[0].Content)["name"].ShouldEqual("decrypted-name");
}
