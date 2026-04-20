// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_event_type_already_exists_and_gets_new_generation : given.all_dependencies
{
    Exception _exception;

    void Establish() =>
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(callInfo =>
            {
                var generation = callInfo.ArgAt<EventTypeGeneration?>(1);

                if (generation is null)
                {
                    return true;
                }

                return generation.Value == 1;
            });

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(new RegisterEventTypesRequest
        {
            EventStore = "test-store",
            Types =
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 2 },
                    Schema = "{}",
                    Migrations =
                    {
                        new EventTypeMigrationDefinition
                        {
                            FromGeneration = 1,
                            ToGeneration = 2,
                            UpcastJmesPath = "{}",
                            DowncastJmesPath = "{}"
                        }
                    },
                    Generations =
                    {
                        new EventTypeGenerationDefinition { Generation = 1, Schema = "{}" },
                        new EventTypeGenerationDefinition { Generation = 2, Schema = "{}" }
                    }
                }
            ]
        }));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_append_event_type_generation_added_system_event() =>
        _systemEventSequence.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == "some-event"),
            Arg.Is<EventTypeGenerationAdded>(@event =>
                @event.EventTypeId.Value == "some-event" &&
                @event.Generation.Value == 2 &&
                @event.Schema == "{}"));
    [Fact] void should_not_append_event_type_added_system_event() =>
        _systemEventSequence.DidNotReceive().Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<EventTypeAdded>());
}
