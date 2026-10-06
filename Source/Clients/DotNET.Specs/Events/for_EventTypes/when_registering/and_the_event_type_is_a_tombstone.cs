// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_the_event_type_is_a_tombstone : given.all_dependencies
{
    [EventType]
    [Tombstone]
    record EntityRemoved;

    EventTypes _subject;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(EntityRemoved)]);
        _subject = new(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_register_the_tombstone_marker() => _eventTypesService.Received(1).RegisterEventTypes(Arg.Is<RegisterEventTypesRequest>(_ => _.Types.Single().Type.Tombstone));
    [Fact] void should_carry_the_marker_in_the_discovered_event_type() => _subject.GetEventTypeFor(typeof(EntityRemoved)).Tombstone.ShouldBeTrue();
    [Fact] void should_carry_the_marker_in_the_append_contract() => _subject.GetEventTypeFor(typeof(EntityRemoved)).ToSequencesContract().Tombstone.ShouldBeTrue();
}
