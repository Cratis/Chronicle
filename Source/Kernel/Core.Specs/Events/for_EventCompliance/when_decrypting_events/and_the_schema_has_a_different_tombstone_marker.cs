// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_decrypting_events;

public class and_the_schema_has_a_different_tombstone_marker : given.all_dependencies
{
    AppendedEvent _event;
    AppendedEvent[] _result;

    void Establish() => _event = new AppendedEvent(
        EventContext.Empty with { EventType = SomeEventType, Subject = new Subject(SubjectValue) },
        new ExpandoObject());

    async Task Because()
    {
        var storedType = SomeEventType with { Tombstone = true };
        _result = await _compliance.Release(
            [_event],
            new Dictionary<EventType, EventTypeSchema> { [storedType] = new(storedType, EventTypeOwner.Client, EventTypeSource.Code, _schemaWithPii) });
    }

    [Fact] void should_release_compliance() => _complianceManager.Received(1).Release(_event.Context.EventStore, _event.Context.Namespace, _schemaWithPii, SubjectValue, Arg.Any<JsonObject>());
    [Fact] void should_return_the_decrypted_content() => ((IDictionary<string, object?>)_result[0].Content)["name"].ShouldEqual("decrypted-name");
}
