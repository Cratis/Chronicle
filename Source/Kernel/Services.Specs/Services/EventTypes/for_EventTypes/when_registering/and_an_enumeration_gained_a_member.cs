// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

/// <summary>
/// An enumeration frequently mirrors something the application does not own, and gaining a member does not change
/// what any already stored value means. Registration must let the generation take the wider member list rather than
/// rejecting it as <see cref="EventTypeSchemaChanged"/> and forcing a generation for something history never noticed.
/// </summary>
public class and_an_enumeration_gained_a_member : given.all_dependencies
{
    const string StoredSchema = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Verified"]}}}""";
    const string WiderSchema = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1,2],"x-enumNames":["Unknown","Verified","Revoked"]}}}""";

    Exception _exception;

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, StoredSchema)));

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(new RegisterEventTypesRequest
        {
            EventStore = "test-store",
            Types =
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1 },
                    Schema = WiderSchema,
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = WiderSchema }
                    }
                }
            ]
        }));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();

    [Fact] void should_store_the_wider_member_list() =>
        _eventTypesStorage.Received(1).Register(Arg.Is<IEnumerable<Concepts.Events.EventTypeToRegister>>(
            _ => _.Single().Definition.Generations.Single().Schema.ToJson().Contains("Revoked")));
}
