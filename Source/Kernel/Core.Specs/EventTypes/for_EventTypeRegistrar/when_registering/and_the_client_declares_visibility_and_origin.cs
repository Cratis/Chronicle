// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_the_client_declares_visibility_and_origin : given.all_dependencies
{
    List<EventTypeToRegister> _registered;

    void Establish()
    {
        _registered = [];
        _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>())
            .Returns(call => Capture(call.Arg<IEnumerable<EventTypeToRegister>>()));
    }

    async Task Because() =>
        await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "public-event", Generation = 1 },
                    Schema = "{}",
                    EventStore = "owning-store",
                    Visibility = Contracts.Events.EventTypeVisibility.Public
                },
                new EventTypeRegistration
                {
                    Type = new() { Id = "private-event", Generation = 1 },
                    Schema = "{}",
                    Visibility = Contracts.Events.EventTypeVisibility.Private
                },
                new EventTypeRegistration
                {
                    Type = new() { Id = "old-client-event", Generation = 1 },
                    Schema = "{}"
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture);

    [Fact] void should_hand_the_public_visibility_to_storage() => Registered("public-event").Visibility.ShouldEqual(Concepts.Events.EventTypeVisibility.Public);
    [Fact] void should_hand_the_origin_to_storage() => Registered("public-event").Origin.ShouldEqual("owning-store");
    [Fact] void should_hand_the_private_visibility_to_storage() => Registered("private-event").Visibility.ShouldEqual(Concepts.Events.EventTypeVisibility.Private);
    [Fact] void should_treat_a_client_that_sends_nothing_as_unspecified() => Registered("old-client-event").Visibility.ShouldEqual(Concepts.Events.EventTypeVisibility.Unspecified);
    [Fact] void should_treat_a_client_that_sends_no_origin_as_the_registering_store() => Registered("old-client-event").Origin.ShouldEqual(string.Empty);

    IEnumerable<EventTypeId> Capture(IEnumerable<EventTypeToRegister> types)
    {
        _registered.AddRange(types);
        return [];
    }

    EventTypeToRegister Registered(string id) => _registered.Single(_ => _.Definition.Id.Value == id);
}
