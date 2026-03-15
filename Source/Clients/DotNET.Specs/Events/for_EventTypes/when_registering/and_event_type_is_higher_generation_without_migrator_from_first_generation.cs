// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

[EventType(generation: 3)]
file record Gen3TestEventMissingStart(string Name);

public class and_event_type_is_higher_generation_without_migrator_from_first_generation : given.all_dependencies
{
    EventTypes _subject;
    Exception _exception;

    void Establish()
    {
        var migrator = Substitute.For<IEventTypeMigration>();
        migrator.From.Returns(new EventTypeGeneration(2));
        migrator.To.Returns(new EventTypeGeneration(3));

        _clientArtifacts.EventTypes.Returns([typeof(Gen3TestEventMissingStart)]);
        _eventTypeMigrators.GetMigratorsFor(typeof(Gen3TestEventMissingStart)).Returns([migrator]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.Discover();
        _exception = await Catch.Exception(_subject.Register);
    }

    [Fact] void should_throw() => _exception.ShouldNotBeNull();
    [Fact] void should_throw_missing_first_generation_for_event_type() => _exception.ShouldBeOfExactType<MissingFirstGenerationForEventType>();
}
