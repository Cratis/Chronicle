// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_event_type_is_a_tombstone : given.all_dependencies
{
    async Task Because() =>
        await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1, Tombstone = true },
                    Schema = "{}"
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture);

    [Fact] void should_store_the_tombstone_marker() =>
        _eventTypesStorage.Received(1).Register(Arg.Is<IEnumerable<Concepts.Events.EventTypeToRegister>>(_ => _.Single().Definition.Tombstone));
}
