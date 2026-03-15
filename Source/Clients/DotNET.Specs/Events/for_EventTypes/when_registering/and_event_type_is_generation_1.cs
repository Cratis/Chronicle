// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_event_type_is_generation_1 : given.all_dependencies
{
    [EventType]
    record Gen1TestEvent(string Name);

    EventTypes _subject;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(Gen1TestEvent)]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_not_throw() => true.ShouldBeTrue();
    [Fact] void should_have_registered_event_types() => _eventTypesService.Received(1).Register(Arg.Any<RegisterEventTypesRequest>());
}
