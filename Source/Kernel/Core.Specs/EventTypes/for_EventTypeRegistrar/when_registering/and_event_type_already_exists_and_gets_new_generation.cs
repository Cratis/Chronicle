// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_event_type_already_exists_and_gets_new_generation : given.all_dependencies
{
    Exception _exception;

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, "{}")));

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 2 },
                    Schema = "{}",
                    Migrations =
                    {
                        new Contracts.Events.EventTypeMigrationDefinition
                        {
                            FromGeneration = 1,
                            ToGeneration = 2,
                            UpcastJmesPath = "{}",
                            DowncastJmesPath = "{}"
                        }
                    },
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = "{}" },
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 2, Schema = "{}" }
                    }
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture));

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
