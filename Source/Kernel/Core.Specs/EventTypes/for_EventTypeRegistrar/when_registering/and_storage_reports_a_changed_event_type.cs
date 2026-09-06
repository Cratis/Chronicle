// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_storage_reports_a_changed_event_type : given.all_dependencies
{
    void Establish() =>
        _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>())
            .Returns([new EventTypeId("changed-event")]);

    async Task Because() =>
        await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "changed-event", Generation = 1 },
                    Schema = "{}"
                },
                new EventTypeRegistration
                {
                    Type = new() { Id = "unchanged-event", Generation = 1 },
                    Schema = "{}"
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture);

    [Fact] void should_register_every_event_type_in_one_call() =>
        _eventTypesStorage.Received(1).Register(Arg.Is<IEnumerable<EventTypeToRegister>>(_ => _.Count() == 2));
    [Fact] void should_invalidate_the_changed_event_type() =>
        _eventTypesCacheClient.Received(1).Invalidate(
            Arg.Any<EventStoreName>(),
            Arg.Is<EventTypeId>(_ => _.Value == "changed-event"));
    [Fact] void should_not_invalidate_the_unchanged_event_type() =>
        _eventTypesCacheClient.DidNotReceive().Invalidate(
            Arg.Any<EventStoreName>(),
            Arg.Is<EventTypeId>(_ => _.Value == "unchanged-event"));
}
