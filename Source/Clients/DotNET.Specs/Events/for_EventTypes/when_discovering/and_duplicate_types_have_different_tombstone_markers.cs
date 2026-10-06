// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypes.when_discovering;

public class and_duplicate_types_have_different_tombstone_markers : given.all_dependencies
{
    [EventType("duplicate-event")]
    record OrdinaryEvent;

    [EventType("duplicate-event")]
    [Tombstone]
    record TombstoneEvent;

    EventTypes _subject;
    Exception _exception;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(OrdinaryEvent), typeof(TombstoneEvent)]);
        _subject = new(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because() => _exception = await Catch.Exception(_subject.Discover);

    [Fact] void should_reject_the_duplicate_identity() => _exception.ShouldBeOfExactType<MultipleEventTypesWithSameIdFound>();
}
