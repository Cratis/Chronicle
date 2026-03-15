// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

[EventType(generation: 3)]
file record Gen3TestEventWithGap(string Name);

public class and_event_type_has_gap_in_migration_chain : given.all_dependencies
{
    EventTypes _subject;
    Exception _exception;

    void Establish()
    {
        // Only has 1→2 migrator; 2→3 is missing
        var migrator = Substitute.For<IEventTypeMigration>();
        migrator.From.Returns(new EventTypeGeneration(1));
        migrator.To.Returns(new EventTypeGeneration(2));

        _clientArtifacts.EventTypes.Returns([typeof(Gen3TestEventWithGap)]);
        _eventTypeMigrators.GetMigratorsFor(typeof(Gen3TestEventWithGap)).Returns([migrator]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.Discover();
        _exception = await Catch.Exception(_subject.Register);
    }

    [Fact] void should_throw() => _exception.ShouldNotBeNull();
    [Fact] void should_throw_missing_migration_for_generation() => _exception.ShouldBeOfExactType<MissingMigrationForEventTypeGeneration>();
}
