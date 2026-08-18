// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_event_type_has_no_migrators : given.all_dependencies
{
    Exception _exception;

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 2 },
                    Schema = "{}"
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient));

    [Fact] void should_throw_missing_event_type_migrators() => _exception.ShouldBeOfExactType<MissingEventTypeMigrators>();
}
