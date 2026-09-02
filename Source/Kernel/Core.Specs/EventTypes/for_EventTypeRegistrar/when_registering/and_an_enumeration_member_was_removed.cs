// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

/// <summary>
/// Growing an enumeration is safe; losing a member is not. Events already stored with the removed value would be
/// left denoting nothing, so this stays a schema change that needs a new generation - and a value map to say what
/// the departed value became.
/// </summary>
public class and_an_enumeration_member_was_removed : given.all_dependencies
{
    const string StoredSchema = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1,2],"x-enumNames":["Unknown","Verified","Revoked"]}}}""";
    const string NarrowerSchema = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Verified"]}}}""";

    Exception _exception;

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, StoredSchema)));

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1 },
                    Schema = NarrowerSchema,
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = NarrowerSchema }
                    }
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture));

    [Fact] void should_throw_event_type_schema_changed() => _exception.ShouldBeOfExactType<EventTypeSchemaChanged>();
}
