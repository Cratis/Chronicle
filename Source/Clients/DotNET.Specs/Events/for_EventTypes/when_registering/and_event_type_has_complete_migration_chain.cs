// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

[EventType(generation: 2)]
file record Gen2TestEventWithValidChain(string Name);

public class and_event_type_has_complete_migration_chain : given.all_dependencies
{
    EventTypes _subject;

    void Establish()
    {
        var migrator = Substitute.For<IEventTypeMigration>();
        migrator.From.Returns(new EventTypeGeneration(1));
        migrator.To.Returns(new EventTypeGeneration(2));

        _clientArtifacts.EventTypes.Returns([typeof(Gen2TestEventWithValidChain)]);
        _eventTypeMigrators.GetMigratorsFor(typeof(Gen2TestEventWithValidChain)).Returns([migrator]);
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
