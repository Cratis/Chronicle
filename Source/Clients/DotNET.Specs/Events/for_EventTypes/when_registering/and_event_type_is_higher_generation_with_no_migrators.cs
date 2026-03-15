// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

[EventType(generation: 2)]
file record Gen2TestEvent(string Name);

public class and_event_type_is_higher_generation_with_no_migrators : given.all_dependencies
{
    EventTypes _subject;
    Exception _exception;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(Gen2TestEvent)]);
        _eventTypeMigrators.GetMigratorsFor(typeof(Gen2TestEvent)).Returns([]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.Discover();
        _exception = await Catch.Exception(_subject.Register);
    }

    [Fact] void should_throw() => _exception.ShouldNotBeNull();
    [Fact] void should_throw_missing_event_type_migrators() => _exception.ShouldBeOfExactType<MissingEventTypeMigrators>();
}
