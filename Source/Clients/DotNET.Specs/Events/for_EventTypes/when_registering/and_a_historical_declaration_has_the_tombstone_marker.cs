// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_a_historical_declaration_has_the_tombstone_marker : given.all_dependencies
{
    [EventType(generation: 2)]
    record EntityRemoved;

    [EventTypeGenerationFor<EntityRemoved>(1)]
    [Tombstone]
    record EntityRemovedV1;

    EventTypes _subject;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(EntityRemoved), typeof(EntityRemovedV1)]);
        _subject = new(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_register_the_declared_marker() => _eventTypesService.Received(1).RegisterEventTypes(Arg.Is<RegisterEventTypesRequest>(_ => _.Types.Single().Type.Tombstone));
    [Fact] void should_keep_the_current_generation() => _eventTypesService.Received(1).RegisterEventTypes(Arg.Is<RegisterEventTypesRequest>(_ => _.Types.Single().Type.Generation == 2));
}
