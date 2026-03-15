// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

[EventType(generation: 2)]
file record Gen2TestEventValidationDisabled(string Name);

public class and_validation_is_disabled : given.all_dependencies
{
    EventTypes _subject;

    void Establish()
    {
        // No migrators registered — would normally throw MissingEventTypeMigrators
        _clientArtifacts.EventTypes.Returns([typeof(Gen2TestEventValidationDisabled)]);
        _eventTypeMigrators.GetMigratorsFor(typeof(Gen2TestEventValidationDisabled)).Returns([]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators, disableEventTypeGenerationValidation: true);
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_not_throw() => true.ShouldBeTrue();
    [Fact] void should_have_registered_event_types() => _eventTypesService.Received(1).Register(Arg.Any<RegisterEventTypesRequest>());
}
