// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypes.when_discovering;

public class and_two_types_represent_the_same_generation_of_an_event_type : given.all_dependencies
{
    [EventType("shared-event", generation: 2)]
    record SharedEventV2(string Name);

    [EventTypeGenerationFor<SharedEventV2>(1)]
    record SharedEventV1A(string Name);

    [EventTypeGenerationFor<SharedEventV2>(1)]
    record SharedEventV1B(string Value);

    EventTypes _subject;
    Exception _exception;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(SharedEventV1A), typeof(SharedEventV1B), typeof(SharedEventV2)]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because() => _exception = await Catch.Exception(_subject.Discover);

    [Fact] void should_throw() => _exception.ShouldNotBeNull();
    [Fact] void should_throw_multiple_event_types_with_same_id_found() => _exception.ShouldBeOfExactType<MultipleEventTypesWithSameIdFound>();
}
