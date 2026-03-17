// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypes.when_discovering;

public class and_two_event_types_share_same_id_and_generation : given.all_dependencies
{
    [EventType("duplicate-event", generation: 1)]
    record DuplicateEventA(string Name);

    [EventType("duplicate-event", generation: 1)]
    record DuplicateEventB(string Value);

    EventTypes _subject;
    Exception _exception;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(DuplicateEventA), typeof(DuplicateEventB)]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because() => _exception = await Catch.Exception(_subject.Discover);

    [Fact] void should_throw() => _exception.ShouldNotBeNull();
    [Fact] void should_throw_multiple_event_types_with_same_id_found() => _exception.ShouldBeOfExactType<MultipleEventTypesWithSameIdFound>();
}
