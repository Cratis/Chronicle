// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_RegisterSingleEventType.when_registering;

public class and_the_client_declares_visibility_and_origin : Patterns.for_PatternCapture.given.a_pattern_capture_with_a_failing_namespace
{
    RegisterSingleEventType _command;

    void Establish()
    {
        _command = new(_eventStore, new Contracts.Events.EventTypeRegistration
        {
            Type = new() { Id = "CustomerNamed", Generation = 1 },
            Schema = "{}",
            EventStore = "owning-store",
            Visibility = Contracts.Events.EventTypeVisibility.Public
        });
        _eventTypes.Register(Arg.Any<EventType>(), Arg.Any<JsonSchema>(), Arg.Any<EventTypeOwner>(), Arg.Any<EventTypeSource>(), Arg.Any<EventTypeVisibility>(), Arg.Any<string>()).Returns(false);
    }

    async Task Because() => await _command.Handle(_storage, Substitute.For<IEventTypesCacheClient>(), _capture);

    [Fact] void should_hand_visibility_and_origin_to_storage() =>
        _eventTypes.Received(1).Register(
            Arg.Any<EventType>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<EventTypeOwner>(),
            Arg.Any<EventTypeSource>(),
            EventTypeVisibility.Public,
            "owning-store");
}
