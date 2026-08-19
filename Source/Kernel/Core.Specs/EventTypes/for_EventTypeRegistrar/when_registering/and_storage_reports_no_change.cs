// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

internal class and_storage_reports_no_change : given.all_dependencies
{
    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, "{}")));

    async Task Because() =>
        await _subject.Register("test-store", [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1 },
                    Schema = "{}",
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = "{}" }
                    }
                }
            ], false, _storage, _eventTypesCacheClient);

    [Fact] void should_not_invalidate_any_event_type() =>
        _eventTypesCacheClient.DidNotReceive().Invalidate(Arg.Any<EventStoreName>(), Arg.Any<EventTypeId>());
    [Fact] void should_not_append_any_system_event() =>
        _systemEventSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
}
